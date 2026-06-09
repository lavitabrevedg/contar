using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class Firebase_Manager : MonoBehaviour
{
    public static Firebase_Manager Instance { get; private set; }

    [SerializeField] private StageProgressService progressService;
    [SerializeField] private bool readDataOnLogin = true;
    [SerializeField] private bool writeDataOnProgressChanged = true;

    // 파이어베이스 인증 객체
    private FirebaseAuth auth;
    //현재 로그인한 사용자 객체
    private FirebaseUser currentUser;
    // 파이어베이스 데이터베이스 참조 객체
    private DatabaseReference reference;
    private DatabaseReference userProgressReference;
    private bool isInitialized;
    private bool isProgressChangedSubscribed;
    private bool isApplyingRemoteProgress;

    public DatabaseReference Reference => reference;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveProgressService();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (progressService != null && isProgressChangedSubscribed)
            progressService.PersistentProgressChanged -= OnProgressChanged;

        Instance = null;
    }

    public void Init()
    {
        // Firebase SDK의 모든 필수 구성요소가 있는지 확인하고 없으면 수정
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[Firebase_Manager] Firebase 초기화 실패: " + task.Exception);
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("[Firebase_Manager] Firebase 의존성 사용 불가: " + task.Result);
                return;
            }

            // 파이어베이스 인증 객체 초기화
            auth = FirebaseAuth.DefaultInstance;
            // 현재 로그인한 사용자 객체 초기화
            currentUser = auth.CurrentUser;
            // 파이어베이스 데이터베이스 참조 객체 초기화
            reference = FirebaseDatabase.DefaultInstance.RootReference;
            isInitialized = true;

            GuestLogin();
            Debug.Log("[Firebase_Manager] Firebase 초기화 성공!");
        });
    }

    public void GuestLogin() //게스트 로그인
    {
        if (!isInitialized)
            return;

        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
            PrepareUserProgressReference();

            Debug.Log("[Firebase_Manager] 이미 로그인 상태입니다. : " + currentUser.UserId);

            if (readDataOnLogin)
                ReadData();

            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[Firebase_Manager] 게스트 로그인 실패: " + task.Exception);
                return;
            }

            currentUser = task.Result.User;
            PrepareUserProgressReference();

            // Unique ID
            Debug.Log("[Firebase_Manager] 게스트 로그인 성공! 사용자 ID : " + currentUser.UserId);

            if (readDataOnLogin)
                ReadData();
        });
    }

    public void ReadData()
    {
        if (!CanUseDatabase())
            return;

        userProgressReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[Firebase_Manager] 데이터 읽기 실패: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                WriteData();
                SubscribeProgressChanged();
                return;
            }

            string json = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(json))
            {
                WriteData();
                SubscribeProgressChanged();
                return;
            }

            StageProgressSnapshot remoteProgress = JsonUtility.FromJson<StageProgressSnapshot>(json);
            StageProgressSnapshot localProgress = progressService.CreateSnapshot();

            if (ShouldUseRemoteProgress(remoteProgress, localProgress))
            {
                isApplyingRemoteProgress = true;
                progressService.ApplySnapshot(remoteProgress);
                isApplyingRemoteProgress = false;
            }
            else
            {
                WriteData();
            }

            SubscribeProgressChanged();
        });
    }

    public void WriteData()
    {
        if (!CanUseDatabase())
            return;

        progressService.EnsurePersistentTimestamp();
        StageProgressSnapshot progress = progressService.CreateSnapshot();
        string json = JsonUtility.ToJson(progress);

        userProgressReference.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[Firebase_Manager] 데이터 쓰기 실패: " + task.Exception);
                return;
            }

            Debug.Log("[Firebase_Manager] 데이터 쓰기 성공.");
        });
    }

    private void PrepareUserProgressReference()
    {
        userProgressReference = reference
            .Child("users")
            .Child(currentUser.UserId)
            .Child("progress");
    }

    private bool CanUseDatabase()
    {
        if (!isInitialized)
            return false;

        if (currentUser == null)
            return false;

        if (userProgressReference == null)
            return false;

        ResolveProgressService();

        return progressService != null;
    }

    private bool ShouldUseRemoteProgress(StageProgressSnapshot remoteProgress, StageProgressSnapshot localProgress)
    {
        if (remoteProgress == null)
            return false;

        if (remoteProgress.highestClearedStageIndex > localProgress.highestClearedStageIndex)
            return true;

        if (remoteProgress.highestClearedStageIndex < localProgress.highestClearedStageIndex)
            return false;

        return remoteProgress.updatedAtUtcTicks > localProgress.updatedAtUtcTicks;
    }

    private void SubscribeProgressChanged()
    {
        if (!writeDataOnProgressChanged)
            return;

        ResolveProgressService();

        if (progressService == null)
            return;

        if (isProgressChangedSubscribed)
            return;

        progressService.PersistentProgressChanged += OnProgressChanged;
        isProgressChangedSubscribed = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ChangeProgressService(FindFirstObjectByType<StageProgressService>());

        if (readDataOnLogin && CanUseDatabase())
            ReadData();
    }

    private void ChangeProgressService(StageProgressService nextProgressService)
    {
        if (progressService == nextProgressService)
            return;

        if (progressService != null && isProgressChangedSubscribed)
            progressService.PersistentProgressChanged -= OnProgressChanged;

        progressService = nextProgressService;
        isProgressChangedSubscribed = false;

        if (progressService != null)
            SubscribeProgressChanged();
    }

    private void OnProgressChanged()
    {
        if (isApplyingRemoteProgress)
            return;

        WriteData();
    }

    private void ResolveProgressService()
    {
        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();
    }
}
