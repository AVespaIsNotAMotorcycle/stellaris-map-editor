using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class DirectoryUI : MonoBehaviour
{

    // Directory strings
    string importDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Stellaris\map\setup_scenarios\";
    string exportDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Stellaris\map\setup_scenarios\";
    //string exportDirectory = @"C:\Users\komer\Desktop";
    string workingDirectory = "";
    string mode = "";

    // UI Prefabs
    public GameObject folderPrefab;
    public GameObject filePrefab;

    // UI GameObjects
    public GameObject directoryUIObject;
    public GameObject directoryTextObject;
    public GameObject scrollviewContents;
    public GameObject fileNameObject;

    // openUI : sets the directory, displays and refreshes UI
    // mode : 'export' loads exportDirectory, 'import' loads importDirectory
    public void openUI(string nmode) {
        mode = nmode;
        if (mode == "import") { workingDirectory = importDirectory; };
        if (mode == "export") { workingDirectory = exportDirectory; };

        directoryUIObject.SetActive(true);
        refreshUI();
    }

    void refreshUI() {
        Debug.Log(workingDirectory);
        directoryTextObject.GetComponent<InputField>().text = workingDirectory;

        string[] folders = Directory.GetDirectories(workingDirectory);
        string[] files = Directory.GetFiles(workingDirectory);

        // Clear existing Viewport Contents
        List<GameObject> children = new List<GameObject>();
        for (int i = 0; i < scrollviewContents.transform.childCount; i++) {
            children.Add(scrollviewContents.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < children.Count; i++) {
            Destroy(children[i]);
        }

        int ticker = 0;
        float width = scrollviewContents.transform.parent.parent.gameObject.GetComponent<RectTransform>().rect.width;
        // Create a button for each folder
        for (int i = 0; i < folders.Length; i++) {
            GameObject clone = Instantiate(folderPrefab, scrollviewContents.transform.position, scrollviewContents.transform.rotation);
            clone.transform.SetParent(scrollviewContents.transform);
            clone.GetComponent<RectTransform>().sizeDelta = new Vector2(width,30);
           // clone.GetComponent<RectTransform>().offsetMin = new Vector2(0,0);
           // clone.GetComponent<RectTransform>().offsetMax = new Vector2(0,30);
           // clone.transform.position -= new Vector3(0, 30 + (ticker * 30), 0);
            clone.transform.GetChild(0).gameObject.GetComponent<Text>().text = folders[i].Substring(workingDirectory.Length);
            clone.GetComponent<Button>().onClick.AddListener(delegate {selectFolder(clone.transform.GetChild(0).gameObject.GetComponent<Text>().text);});
            ticker++;
        }
        
        // Create a text object for each file
        for (int i = 0; i < files.Length; i++) {
            GameObject clone = Instantiate(filePrefab, scrollviewContents.transform.position, scrollviewContents.transform.rotation);
            clone.transform.SetParent(scrollviewContents.transform);
            clone.GetComponent<RectTransform>().sizeDelta = new Vector2(width,30);
            //clone.GetComponent<RectTransform>().offsetMin = new Vector2(0,0);
            //clone.GetComponent<RectTransform>().offsetMax = new Vector2(0,30);
            //clone.transform.position -= new Vector3(0, 30 + (ticker * 30), 0);
            clone.transform.GetChild(0).gameObject.GetComponent<Text>().text = files[i].Substring(workingDirectory.Length);
            clone.GetComponent<Button>().onClick.AddListener(delegate {selectFile(clone.transform.GetChild(0).gameObject.GetComponent<Text>().text);});
            ticker++;
        }
        GameObject.Find("CameraController").GetComponent<CameraMotion>().FetchInputFields();    // Update camera's array of InputFields
    }

    public void closeUI() {
        directoryUIObject.SetActive(false);
    }

    public void selectFolder(string folderName) {
        Debug.LogFormat("Selected {0}", folderName);
        workingDirectory += folderName + @"\";
        refreshUI();
    }

    public void selectFile(string fileName) {
        Debug.LogFormat("Selected {0}", fileName);
        fileNameObject.GetComponent<InputField>().text = fileName;
    }

    public void backButton() {
        
        string tempDirectory = workingDirectory;
        if (tempDirectory.Substring(tempDirectory.Length - 2) != @":\") {
            tempDirectory = tempDirectory.Substring(0, tempDirectory.Length - 1);
            while(tempDirectory[tempDirectory.Length - 1] != '\\') {
                tempDirectory = tempDirectory.Substring(0, tempDirectory.Length - 1);
                Debug.Log(tempDirectory);
            }
            workingDirectory = tempDirectory;
            refreshUI();
        }
        else {
            Debug.Log(" At C:\\");
        }

    }

    public void confirmSelection() {
        if (mode == "import") {
            string fileName = fileNameObject.GetComponent<InputField>().text;
            gameObject.GetComponent<MapEditor>().ImportMap(workingDirectory + fileName);
        }
        if (mode == "export") {
            string fileName = fileNameObject.GetComponent<InputField>().text;
            gameObject.GetComponent<MapEditor>().ExportMap(workingDirectory + fileName);
        }
        closeUI();
    }

}
