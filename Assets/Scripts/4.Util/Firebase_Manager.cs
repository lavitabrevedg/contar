using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
public partial class Firebase_Manager
{
    // 파이어베이스 인증 객체
    private FirebaseAuth auth;
    //현재 로그인한 사용자 객체
    private FirebaseUser currentUser;
    // 파이어베이스 데이터베이스 참조 객체
    public DatabaseReference reference;

    public void Init()
    {
        // Firebase SDK의 모든 필수 구성요소가 있는지 확인하고 없으면 수정
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                // 파이어베이스 인증 객체 초기화
                auth = FirebaseAuth.DefaultInstance;
                // 현재 로그인한 사용자 객체 초기화
                currentUser = auth.CurrentUser;
                // 파이어베이스 데이터베이스 참조 객체 초기화
                reference = FirebaseDatabase.DefaultInstance.RootReference;

                GuestLogin();
                Debug.Log("Firebase 초기화 성공!");
            }
            else
            {
                Debug.Log("Firebase 초기화 실패: " + task.Exception.ToString());
            }
        });
    }

    public void GuestLogin() //게스트 로그인
    {
        if(auth.CurrentUser != null)
        {
            Debug.Log("이미 로그인 상태입니다. : " + auth.CurrentUser.UserId);
            //ReadData();
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("게스트 로그인 실패: " + task.Exception);
                return;
            }

            FirebaseUser user = task.Result.User;
            // Unique ID
            Debug.Log("게스트 로그인 성공! 사용자 ID : " + user.UserId);
            //ReadData();
        });
    }

    private void ReadData()
    {

    }

    public void WriteData()
    {

    }
}