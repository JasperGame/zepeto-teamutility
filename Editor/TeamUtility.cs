using UnityEngine;
using UnityEditor;
using ZEPETO.Script;
using System;
using System.IO;
using ZEPETO.World.Editor;

[Serializable]
public class TypescriptScriptSet {
    public JavascriptAsset clientScript;
    public DefaultAsset serverScript;
}

[Serializable]
public class DataSet {
    public string key;
    public int type;
    public string value;
}

[FilePath("Assets/Settings/BaseProjectUtilConfigurations.nwb", FilePathAttribute.Location.ProjectFolder)]
public class TeamUtility : ScriptableSingleton<TeamUtility> {
    [SerializeField] public bool useSceneSetting;
    [SerializeField] public bool useDefaultData;
    [SerializeField] public bool useFileSync;
    [SerializeField] public int language;
    [SerializeField] public SceneAsset[] scenesToBuild; // 빌드에 포함 될 씬 목록
    [SerializeField] public int defaultDataType;
    [SerializeField] public DataSet[] defaultData;
    [SerializeField] public TypescriptScriptSet defaultDataFiles; // 기본 데이터를 입력 할 데이터 파일
    [SerializeField] public TypescriptScriptSet[] scriptsToSync; // 클라, 서버 간 싱크 할 파일 목록

    void OnEnable() {
        hideFlags &= ~HideFlags.NotEditable;
    }

    public void Save() {
        Save(true);
    }

    public void RecompileAllZepetoScript() {
        string[] guids = AssetDatabase.FindAssets("t:typescriptasset", null);
        TypescriptAsset[] typescriptAssets = new TypescriptAsset[guids.Length];
        Debug.Log("guid length:" + guids.Length);
        for (var i = 0; i < guids.Length; i++) {
            typescriptAssets[i] =
                AssetDatabase.LoadAssetAtPath<TypescriptAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        TypescriptAssetPostprocessor.Compile(typescriptAssets, true);
        if (this.language == 0) {
            EditorUtility.DisplayDialog($"Team Utility", $"Recompiled {guids.Length.ToString()} Ts scripts.",
                "OK");
        } else {
            EditorUtility.DisplayDialog($"Base Project Utility", $"{guids.Length.ToString()}개의 타입스크립트 파일 리컴파일을 완료했습니다.",
                "확인");
        }
    }

    public void DiscardUnwantedTsMetaFiles() {
        string repositoryRoot = "";
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) {
            repositoryRoot = this.ExecuteCommand("git rev-parse --show-toplevel")[0];
        } else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows) {
            repositoryRoot = this.ExecuteCommand("git rev-parse --show-toplevel")[4];
        }
        string[] gitStatusOutput = this.ExecuteCommand("git status");
        string[] metaFiles = Directory.GetFiles(Application.dataPath, "*.ts.meta", SearchOption.AllDirectories);
        int fileDiscardedCount = 0;
        foreach (string metaFileString in metaFiles) {
            string metaFile = "";
            if(SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows){
                metaFile = metaFileString.Replace(@"\", "/");
            } else {
                metaFile = metaFileString;
            }
            string tsFile = metaFile.Replace(".meta", "");
            if (!File.Exists(tsFile)) {
                continue;
            }
            bool metaUpdated = false;
            foreach (string line in gitStatusOutput) {
                if (line.Contains("modified:")) {
                    string filePath = line.Substring(12).Trim();
                    if (filePath == metaFile.Substring(repositoryRoot.Length + 1)) {
                        metaUpdated = true;
                        break;
                    }
                }
            }
            if (metaUpdated) {
                bool tsUpdated = false;
                foreach (string line in gitStatusOutput) {
                    if (line.Contains("modified:")) {
                        string filePath = line.Substring(12).Trim();
                        if (filePath == tsFile.Substring(repositoryRoot.Length + 1)) {
                            tsUpdated = true;
                            break;
                        }
                    }
                }
                if (!tsUpdated) {
                    string command = $"git restore {metaFile}";
                    this.ExecuteCommandWithoutResult(command);
                    fileDiscardedCount++;
                }
            }
        }
        if (this.language == 0) {
            EditorUtility.DisplayDialog($"Base Project Utility",
                $"{fileDiscardedCount.ToString()} *.ts.meta files temporarily reset. Open your git client and commit your changes while keeping this popup opened",
                "OK");
        } else {
            EditorUtility.DisplayDialog($"Base Project Utility",
                $"{fileDiscardedCount.ToString()}개의 메타파일을 임시로 리셋했습니다. 본 팝업을 띄워둔 채로 Git 클라이언트로 이동하여 커밋을 진행하세요.", "확인");
        }
    }

    public void UpdateSceneSettingForBuild() {
        if ((this.scenesToBuild == null) || (this.scenesToBuild.Length == 0)) {
            if (this.language == 0) {
                Debug.LogWarning("There's no scene assigned");
            } else {
                Debug.LogWarning("빌드에 포함 할 씬 목록을 먼저 설정하세요");
            }
            return;
        }
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[this.scenesToBuild.Length];
        for (var i = 0; i < this.scenesToBuild.Length; i++) {
            EditorBuildSettingsScene newScene = new EditorBuildSettingsScene();
            newScene.path = AssetDatabase.GetAssetPath(this.scenesToBuild[i]);
            newScene.enabled = true;
            scenes[i] = newScene;
        }
        EditorBuildSettings.scenes = scenes;

        if (this.language == 0) {
            EditorUtility.DisplayDialog("Base Project Utility", "Updated scenes in build in Build Setting.", "OK");
        } else {
            EditorUtility.DisplayDialog("Base Project Utility", "빌드 셋팅 내 Scenes in build 업데이트가 완료되었습니다.", "확인");
        }
    }

    public void SyncShared() {
        if (scriptsToSync.Length == 0) {
            if (this.language == 0) {
                Debug.LogWarning("No target script assets has assigned");
            } else {
                Debug.LogWarning("스크립트를 먼저 연결해주세요");
            }
        } else {
            for (int i = 0; i < scriptsToSync.Length; i++) {
                if ((scriptsToSync[i].clientScript == null) || (scriptsToSync[i].serverScript == null)) {
                    if (this.language == 0) {
                        Debug.LogWarning("Check if both client-side and server-side scripts are ready");
                    } else {
                        Debug.LogWarning("싱크 할 클라이언트와 서버 스크립트가 쌍을 이루어야 합니다.");
                    }
                } else {
                    var originalPath = scriptsToSync[i].clientScript.assetPath;
                    var duplicatePath = AssetDatabase.GetAssetPath(scriptsToSync[i].serverScript);
                    StreamReader reader = new StreamReader(originalPath);
                    var text = reader.ReadToEnd();
                    StreamWriter writer = new StreamWriter(duplicatePath, false);
                    writer.Write(text);
                    writer.Close();
                    AssetDatabase.ImportAsset(duplicatePath);
                }
            }
        }
        if (this.language == 0) {
            EditorUtility.DisplayDialog("Base Project Utility", "Sync files finished", "OK");
        } else {
            EditorUtility.DisplayDialog("Base Project Utility", "파일 싱크가 완료되었습니다.", "확인");
        }
    }

    private string[] ExecuteCommand(string command) {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            return output.Split('\n');
        } else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows) {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            return output.Split('\n');
        }
        return null;
    }

    private void ExecuteCommandWithoutResult(string command) {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
        } else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows) {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
        }
    }
}