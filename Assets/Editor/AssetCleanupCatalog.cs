using System.Collections.Generic;
using UnityEditor;

public static class AssetCleanupCatalog
{
    public const string ReportPath = "Logs/AssetCleanupReport.txt";
    public const string PreDeleteFlagPath = "Library/AssetCleanupPreDeletePassed.flag";
    public const string MigrateRequestPath = "Library/AssetCleanupMigrate.request";
    public const string DeleteRequestPath = "Library/AssetCleanupDelete.request";

    public const string CommonUiFolder = "Assets/Art/UI/Common";
    public const string LobbyUiFolder = "Assets/Art/UI/Lobby";
    public const string LobbyStandaloneFolder = "Assets/Art/UI/Lobby/Standalone";
    public const string InGameUiFolder = "Assets/Art/UI/InGame";
    public const string AtlasFolder = "Assets/Art/Atlases";

    public const string CommonUiAtlasPath = "Assets/Art/Atlases/CommonUIAtlas.spriteatlas";
    public const string LobbyUiAtlasPath = "Assets/Art/Atlases/LobbyUIAtlas.spriteatlas";
    public const string InGameUiAtlasPath = "Assets/Art/Atlases/InGameUIAtlas.spriteatlas";
    public const string BoardAtlasPath = "Assets/Art/Atlases/BoardAtlas.spriteatlas";
    public const string OldAtlasPath = "Assets/Art/UI/ContarUI/ContarUiAtlas.spriteatlas";

    public const string LegacySourceFolder = "Assets/2D Casual UI";
    public const string LegacyGuiPath = "Assets/2D Casual UI/Sprite/GUI.png";
    public const string LegacyVibrationPath = "Assets/2D Casual UI/Sprite/VibrationIcon.png";
    public const string SettingsPrefabPath = "Assets/PreFab/SettingsPanel.prefab";

    public static readonly string[] ScenePaths =
    {
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/InGameScene.unity"
    };

    public static readonly IReadOnlyDictionary<string, string> MovePaths = new Dictionary<string, string>
    {
        { "Assets/Art/UI/ContarUI/Sprites/LegacyUi_2.png", "Assets/Art/UI/Common/SettingsPanelBackground.png" },
        { "Assets/Art/UI/ContarUI/Sprites/LegacyUi_21.png", "Assets/Art/UI/InGame/ResultButtonBackground.png" },
        { "Assets/Art/UI/ContarUI/Sprites/LegacyUi_58.png", "Assets/Art/UI/InGame/ResultPanelBackground.png" },
        { "Assets/Art/UI/ContarUI/Sprites/LegacyUi_61.png", "Assets/Art/UI/InGame/RewardedAdIcon.png" },
        { "Assets/Art/UI/ContarUI/Sprites/LegacyUi_67.png", "Assets/Art/UI/Lobby/StageSelectPanelBackground.png" },
        { "Assets/Art/UI/Controls/off.png", "Assets/Art/UI/Common/off.png" },
        { "Assets/Art/UI/Controls/on.png", "Assets/Art/UI/Common/on.png" },
        { "Assets/Art/UI/Controls/purpleButton.png", "Assets/Art/UI/Common/purpleButton.png" },
        { "Assets/Art/UI/Controls/settings_btn.png", "Assets/Art/UI/Common/settings_btn.png" },
        { "Assets/Art/UI/Controls/yellowButton.png", "Assets/Art/UI/Common/yellowButton.png" },
        { "Assets/Art/UI/Controls/blueButton.png", "Assets/Art/UI/Lobby/blueButton.png" },
        { "Assets/Art/UI/Controls/greenButton.png", "Assets/Art/UI/Lobby/greenButton.png" },
        { "Assets/Art/UI/Controls/pinkButton.png", "Assets/Art/UI/Lobby/pinkButton.png" },
        { "Assets/Art/UI/Controls/redo_btn (3).png", "Assets/Art/UI/Lobby/redo_btn.png" },
        { "Assets/Art/UI/Controls/Group.png", "Assets/Art/UI/InGame/Group.png" },
        { "Assets/Art/UI/Controls/home_btn.png", "Assets/Art/UI/InGame/home_btn.png" },
        { "Assets/Art/UI/Controls/settings_btn (2).png", "Assets/Art/UI/InGame/settings_btn.png" },
        { "Assets/Art/UI/Contar.png", "Assets/Art/UI/Lobby/Standalone/Contar.png" },
        { "Assets/Art/UI/CurrStageImage.png", "Assets/Art/UI/Lobby/Standalone/CurrStageImage.png" }
    };

    public static readonly string[] CommonUiSpritePaths =
    {
        "Assets/Art/UI/Common/SettingsPanelBackground.png",
        "Assets/Art/UI/Common/MusicIcon.png",
        "Assets/Art/UI/Common/SoundEffectIcon.png",
        "Assets/Art/UI/Common/VibrationIcon.png",
        "Assets/Art/UI/Common/off.png",
        "Assets/Art/UI/Common/on.png",
        "Assets/Art/UI/Common/purpleButton.png",
        "Assets/Art/UI/Common/settings_btn.png",
        "Assets/Art/UI/Common/yellowButton.png"
    };

    public static readonly string[] LobbyUiSpritePaths =
    {
        "Assets/Art/UI/Lobby/StageSelectPanelBackground.png",
        "Assets/Art/UI/Lobby/blueButton.png",
        "Assets/Art/UI/Lobby/greenButton.png",
        "Assets/Art/UI/Lobby/pinkButton.png",
        "Assets/Art/UI/Lobby/redo_btn.png"
    };

    public static readonly string[] InGameUiSpritePaths =
    {
        "Assets/Art/UI/InGame/ResultButtonBackground.png",
        "Assets/Art/UI/InGame/ResultPanelBackground.png",
        "Assets/Art/UI/InGame/RewardedAdIcon.png",
        "Assets/Art/UI/InGame/Group.png",
        "Assets/Art/UI/InGame/home_btn.png",
        "Assets/Art/UI/InGame/settings_btn.png"
    };

    public static readonly string[] BoardSpritePaths =
    {
        "Assets/Art/Board/AnyExitTile.png",
        "Assets/Art/Board/EmptyTile.png",
        "Assets/Art/Board/EvenExitTile.png",
        "Assets/Art/Board/Movetile.png",
        "Assets/Art/Board/ObstacleTile.png",
        "Assets/Art/Board/OddExitTile.png",
        "Assets/Art/Board/StartTile.png",
        "Assets/Art/Board/WallTile.png",
        "Assets/Art/Characters/Player.png"
    };

    public static readonly string[] FontDeletionPaths =
    {
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Black.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-ExtraBold.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-ExtraLight.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Light.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Medium.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Regular.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-SemiBold.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Thin.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Bold SDF.asset"
    };

    public static readonly string[] RequiredStandalonePaths =
    {
        "Assets/Art/UI/Lobby/Standalone/Contar.png",
        "Assets/Art/UI/Lobby/Standalone/CurrStageImage.png",
        "Assets/Art/Background/GameBackGround.png",
        "Assets/Art/Board/WomholeTile.png",
        "Assets/Art/Effects/Hint/Circle01.png",
        "Assets/Art/Effects/Hint/Circle02.png",
        "Assets/Art/Effects/Hint/Flare01.png",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Bold.ttf",
        "Assets/TextMesh Pro/Fonts/NotoSansKR-Bold Dynamic SDF.asset",
        "Assets/TextMesh Pro/Fonts/Digitalt SDF.asset"
    };

    public static readonly string[] AtlasPaths =
    {
        CommonUiAtlasPath,
        LobbyUiAtlasPath,
        InGameUiAtlasPath,
        BoardAtlasPath
    };

    public static TextureImporterFormat GetUiFormat()
    {
        return TextureImporterFormat.ASTC_4x4;
    }

    public static TextureImporterFormat GetBoardFormat()
    {
        return TextureImporterFormat.ASTC_6x6;
    }
}
