using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using ZEPETO.Script;

[Serializable]
public class LoginData {
    public string id;
    public string pw;
}

public class TeamUtilityWindow : EditorWindow {
    public string[] languageOptions = new string[] { "English", "한국어" };
    public string[] defaultDataOptions = new string[] { "BPSquare's DataWorld" };
    public string[] dataTypes = new string[] { "string", "int", "float", "boolean" };
    public string[] boolTypes = new string[] { "true", "false" };

    public SerializedProperty useSceneSetting;
    public SerializedProperty useDefaultData;
    public SerializedProperty useFileSync;
    public SerializedProperty language;
    public SerializedProperty scenesToBuild;
    public SerializedProperty defaultDataType;
    public SerializedProperty defaultData;
    public SerializedProperty defaultDataFiles;
    public SerializedProperty scriptsToSync;

    [SerializeField]
    private Vector2 scrollPos = Vector2.zero;
    
    private SerializedObject _serializedObject;
    private List<LoginData> _loginData = new List<LoginData>();
    private bool _isLocked = true;

    private static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;   
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        color.a = 0.5f;
        EditorGUI.DrawRect(r, color);
    }

    public void OnBeforeShow() {
        TeamUtility.instance.Save();
        this._serializedObject = new SerializedObject(TeamUtility.instance);
        this.useSceneSetting = this._serializedObject.FindProperty("useSceneSetting");
        this.useDefaultData = this._serializedObject.FindProperty("useDefaultData");
        this.useFileSync = this._serializedObject.FindProperty("useFileSync");
        this.language = this._serializedObject.FindProperty("language");
        this.scenesToBuild = this._serializedObject.FindProperty("scenesToBuild");
        this.defaultDataType = this._serializedObject.FindProperty("defaultDataType");
        this.defaultData = this._serializedObject.FindProperty("defaultData");
        this.defaultDataFiles = this._serializedObject.FindProperty("defaultDataFiles");
        this.scriptsToSync = this._serializedObject.FindProperty("scriptsToSync");
    }

    void OnGUI() {
        if (this._serializedObject == null) {
            this.OnBeforeShow();
        }

        if (this._serializedObject == null) {
            return;
        }
        _serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        var normalTextSize = GUI.skin.label.fontSize;

        GUIStyle multilineStyle = EditorStyles.label;
        multilineStyle.wordWrap = true;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.BeginVertical("box");
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.skin.label.fontSize = 20;
        GUILayout.Label("Team Utility for ZepetoScript");
        EditorGUI.DropShadowLabel(new Rect(-20, 10, 20, 200), "Team Utility for ZepetoScript");
        GUI.skin.label.fontSize = normalTextSize;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Version: 2023.1.20.");
        GUILayout.Space(24);
        GUILayout.Label("Language/언어:");
        this.language.intValue = EditorGUILayout.Popup(this.language.intValue, languageOptions);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        this.ShowLine();

        this.ShowTitle((this.language.intValue == 0) ? "Zepeto Script" : "제페토 스크립트", normalTextSize);
        if (this.language.intValue == 0) {
            EditorGUILayout.HelpBox(
                "Recompile All Ts files: This function should be used when a resolution to an unknown error is required or when generating a TypeScript package build, in order to recompile the complete set of TypeScript files beforehand. \n\nDiscard All Ts meta files: This function should be used when excluding *.ts.meta files prior to committing to Git.",
                MessageType.Info);
        } else {
            EditorGUILayout.HelpBox(
                "Recompile All Ts files: 원인을 알 수 없는 에러 해결이 필요 할 때, 또는 제페토 패키지 빌드를 생성할 때마다 직전에 타입스크립트 전체 파일을 리컴파일하기 위해 사용합니다. \n\nDiscard All Ts meta files: Git에 커밋을 올리기 전 *.ts.meta 파일을 제외해야 할 때에 사용합니다.",
                MessageType.Info);
        }

        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.BeginHorizontal();
        this.CreateButton("Recompile All Ts files", () => { TeamUtility.instance.RecompileAllZepetoScript(); });
        this.CreateButton("Discard All Ts meta files",
            () => { TeamUtility.instance.DiscardUnwantedTsMetaFiles(); });
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.EndHorizontal();

        this.ShowLine();

        // 씬 관련 셋팅을 합니다.
        var refUseSceneSetting = this.useSceneSetting.boolValue;
        this.ShowTitleWithToggle((this.language.intValue == 0) ? "Scene settings" : "씬 셋팅", normalTextSize,
            ref refUseSceneSetting);
        this.useSceneSetting.boolValue = refUseSceneSetting;
        if (this.useSceneSetting.boolValue) {
            GUILayout.Space(8);
            GUI.skin.label.fontSize = normalTextSize;
            if (this.language.intValue == 0) {
                EditorGUILayout.HelpBox(
                    "The scenes listed in the below list should be registered in the \"scenes in build\" setting of Build Setting in the order they are listed.",
                    MessageType.Info);
            } else {
                EditorGUILayout.HelpBox("아래 목록에 등록되어 있는 씬을 순서대로 Build Setting 내 scenes in build에 등록합니다.",
                    MessageType.Info);
            }

            this.BeginContentArea();
            EditorGUILayout.LabelField("Scenes to include in build:");
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(this.scenesToBuild, true);
            EditorGUILayout.EndVertical();
            this.CreateButton("Set Scenes in build settings",
                () => { TeamUtility.instance.UpdateSceneSettingForBuild(); });
            this.EndContentArea();
        }

        this.ShowLine();

        var refUseFileSync = this.useFileSync.boolValue;
        this.ShowTitleWithToggle((this.language.intValue == 0) ? "Sync Shared Files" : "클라/서버 공유 파일 동기화",
            normalTextSize, ref refUseFileSync);
        this.useFileSync.boolValue = refUseFileSync;
        if (this.useFileSync.boolValue) {
            GUILayout.Space(4);
            GUI.skin.label.fontSize = normalTextSize;
            if (this.language.intValue == 0) {
                EditorGUILayout.HelpBox(
                    "Maintain the contents of TypeScript files to be shared between the client and server consistently. Please note that the server file is updated based on the client file.",
                    MessageType.Info);
            } else {
                EditorGUILayout.HelpBox(
                    "클라, 서버 간 공유해야 할 타입스크립트 파일의 내용을 동일하게 유지합니다.\n클라이언트의 파일을 기준으로 서버 파일이 업데이트되는 점에 유의하세요.",
                    MessageType.Info);
            }

            this.BeginContentArea();
            if (this.language.intValue == 0) {
                EditorGUILayout.LabelField("Files to sync:");
            } else {
                EditorGUILayout.LabelField("싱크 할 파일 목록:");
            }

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(this.scriptsToSync, true);
            GUILayout.EndVertical();
            this.CreateButton("Sync Shared Files", () => { TeamUtility.instance.SyncShared(); });
            this.EndContentArea();
        }

        this.ShowLine();

        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck()) {
            this._serializedObject.ApplyModifiedProperties();
            TeamUtility.instance.Save();
        }
    }

    private void ShowTitle(string title, int normalFontSize) {
        GUI.skin.label.fontSize = 14;
        GUI.skin.label.fontStyle = EditorStyles.boldLabel.fontStyle;
        GUILayout.Label(title);
        GUILayout.Space(4);
        GUI.skin.label.fontSize = normalFontSize;
    }

    private void ShowTitleWithToggle(string title, int normalFontSize, ref bool boolValue) {
        GUI.skin.label.fontSize = 14;
        GUI.skin.label.fontStyle = EditorStyles.boldLabel.fontStyle;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title);
        boolValue = EditorGUILayout.Toggle(boolValue);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void ShowLine() {
        GUILayout.Space(4);
        TeamUtilityWindow.DrawUILine(Color.grey);
        GUILayout.Space(4);
    }

    private void BeginContentArea() {
        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.BeginVertical();
    }

    private void EndContentArea() {
        EditorGUILayout.EndVertical();
        GUILayout.Space(16);
        EditorGUILayout.EndHorizontal();
    }

    private void CreateButton(string title, UnityAction action) {
        EditorGUILayout.BeginVertical();
        GUILayout.Space(8);
        if (GUILayout.Button(title, GUILayout.Height(40))) {
            action();
        }

        EditorGUILayout.EndVertical();
    }
}

static class TeamUtilityMenuItems {
    [MenuItem("TeamUtility/Sync Shared Files", false, 10)]
    static void SyncSharedFiles() {
        TeamUtility.instance.SyncShared();
    }

    [MenuItem("TeamUtility/Recompile All Zepeto Script &#r", false, 30)]
    static void RecompileAll() {
        TeamUtility.instance.RecompileAllZepetoScript();
    }

    [MenuItem("TeamUtility/Discard Unwanted Script Metafiles", false, 30)]
    static void DiscardAll() {
        TeamUtility.instance.DiscardUnwantedTsMetaFiles();
    }

    [MenuItem("TeamUtility/Open Team Utility Window", false, 50)]
    static void Open() {
        TeamUtilityWindow window = (TeamUtilityWindow)EditorWindow.GetWindow(typeof(TeamUtilityWindow));

        window.OnBeforeShow();
        window.Show();
    }
}