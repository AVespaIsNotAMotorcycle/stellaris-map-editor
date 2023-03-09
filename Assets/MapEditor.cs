using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapEditor : MonoBehaviour
{

    #region Variables
    // Engine Management
    public int mode = 0; // 0 : inspect, 1 : place systems, 2 : place lanes, 3 : place nebulae
    public Button[] mode_buttons = new Button[3];
    public Color button_normal_color = new Color(1f, 1f, 1f);
    public GameObject sysprefab;
    public GameObject nebprefab;
    public GameObject sys_info_panel;
    public GameObject edit_ui;
    public GameObject open_edit_ui;
    public GameObject quit_editor_popup;
    public int selected_system;
    public GameObject selected_system_object;

    // UI Raycasting
    public Canvas m_canvas;
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;
    public GameObject m_NebulaTracking;

    // Lane Placement
    bool laneplace_primed = false;
    StarSystem start_point;
    public GameObject lane_obj;

    // Settings 
    [SerializeField]
    string map_name;
    [SerializeField]
    int map_priority;
    [SerializeField]
    bool is_default;
    [SerializeField]
    int min_empires;
    [SerializeField]
    int max_empires;
    [SerializeField]
    int num_empire_default;
    [SerializeField]
    int fallen_empire_default;
    [SerializeField]
    int fallen_empire_max;
    [SerializeField]
    int advanced_empire_default;
    [SerializeField]
    float colonizable_planet_odds;
    [SerializeField]
    bool random_hyperlanes;
    [SerializeField]
    int core_radius;

    public List<StarSystem> systems;
    public List<Hyperlane> hyperlanes;
    public List<Nebula> nebulae;
    #endregion

    #region Monobehaviour Methods
    // Start is called before the first frame update
    void Start()
    {
        //Fetch the Raycaster from the GameObject
        m_Raycaster = m_canvas.GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = m_canvas.GetComponent<EventSystem>();
    }

    // Update is called once per frame
    void Update()
    {

        // ---- Check if the mouse is over any UI elements ----
        //Set up the new Pointer Event
        m_PointerEventData = new PointerEventData(m_EventSystem);
        //Set the Pointer Event Position to that of the mouse position
        m_PointerEventData.position = Input.mousePosition;
        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();
        //Raycast using the Graphics Raycaster and mouse click position
        m_Raycaster.Raycast(m_PointerEventData, results);

        if (results.Count == 0) {
            switch (mode)
            {
                
                default: Inspect(); m_NebulaTracking.SetActive(false); break;

                case 1: PlaceSystems(); m_NebulaTracking.SetActive(false); break;

                case 2: PlaceLanes(); m_NebulaTracking.SetActive(false); break;

                case 3: PlaceNebulae(); m_NebulaTracking.SetActive(true); break;

            }
        }
    }
    #endregion

    // Default editor mode
    // Can click on systems and lanes to view information about them and modify them
    void Inspect() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("Ground", "System", "UI");

            if (Physics.Raycast(ray, out hit, 100, mask)) {
                // Do nothing on UI hit
                //Debug.Log(hit.collider.gameObject.layer);
                if (hit.collider.gameObject.layer == 5) {
                    return;
                }
                // If hit system, select system
                if (hit.collider.gameObject.layer == 9) {
                    selected_system_object = hit.collider.gameObject;
                    Vector2 hitpos = new Vector2(selected_system_object.transform.position.x, selected_system_object.transform.position.z);
                    for (int i = 0; i < systems.Count; i++) {
                        if (systems[i].GetPos() == hitpos) {
                            selected_system = i;
                            break;
                        }
                    }
                    sys_info_panel.SetActive(true);
                    sys_info_panel.transform.GetChild(1).GetComponent<InputField>().text = systems[selected_system].GetName();
                    sys_info_panel.transform.GetChild(3).GetComponent<InputField>().text = systems[selected_system].GetInitializer();
                    GameObject.Find("CameraController").GetComponent<CameraMotion>().FetchInputFields();    // Update camera's array of InputFields
                    return;
                }
                // If hit ground, deselect
                if (hit.collider.gameObject.layer == 8) {
                    selected_system = -1;
                    sys_info_panel.SetActive(false);
                    return;
                }
            }
        }
    }

    public void SetSysName() {
        Debug.Log("SetSysName");
        string name = sys_info_panel.transform.GetChild(1).GetComponent<InputField>().text;
        systems[selected_system].SetName(name);
        selected_system_object.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
    }
    public void SetSysInitializer() {
        Debug.Log("SetSysInit");
        string initializer = sys_info_panel.transform.GetChild(3).GetComponent<InputField>().text;
        systems[selected_system].SetInitializer(initializer);
    }

    #region Map Placement

    // Edits map placement of systems
    // Left click to add
    // Right click to remove
    void PlaceSystems() {

        // Left Click to place systems
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("Ground", "UI");

            if (Physics.Raycast(ray, out hit, 100, mask)) {
                if (hit.collider.gameObject.layer == 5) {
                    return;
                }

                CreateSystem(hit.point);
            }
        }

        // Right click to remove
        if (Input.GetMouseButtonDown(1)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("System", "UI");

            if (Physics.Raycast(ray, out hit, 100, mask)) {
                if (hit.collider.gameObject.layer == 5) {
                    return;
                }

                DeleteSystem(hit.collider.transform.position, hit.collider.gameObject);
            }
        }

    }

    // Click on two systems to create a lane between them
    // Right click on a lane to delete it
    void PlaceLanes() {
        // Left Click to place systems
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("System", "UI");

            if (Physics.Raycast(ray, out hit, 100, mask)) {

                if (hit.collider.gameObject.layer == 5) {
                    return;
                }
                // If a start point has already been selected, create the lane
                if (laneplace_primed) {
                    Vector2 npos = new Vector2(hit.collider.transform.position.x, hit.collider.transform.position.z); 
                    for (int i = 0; i < systems.Count; i++) {
                        if (systems[i].GetPos() == npos) {

                            CreateLane(npos, systems[i]);

                            return;
                        }
                    }
                }
                // Otherwise assign this system to be the start of the lane
                else {
                    laneplace_primed = true;
                    Vector2 npos = new Vector2(hit.collider.transform.position.x, hit.collider.transform.position.z); 
                    for (int i = 0; i < systems.Count; i++) {
                        if (systems[i].GetPos() == npos) {
                            start_point = systems[i];
                            return;
                        }
                    }
                }
            }
        }

        // Right click to remove hyperlane
        if (Input.GetMouseButtonDown(1)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("Hyperlane", "UI");

            if (Physics.Raycast(ray, out hit, 100, mask)) {
                if (hit.collider.gameObject.layer == 5) {
                    return;
                }

                DeleteLane(hit.collider.gameObject);
            }
        }
    }

    void PlaceNebulae() {
        float scrollspeed = 60f;
        float deltaz = Input.GetAxis("Mouse ScrollWheel") * scrollspeed * Time.deltaTime;
        m_NebulaTracking.transform.localScale += new Vector3(deltaz, 0, deltaz);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask mask = LayerMask.GetMask("Ground", "Nebula");

        if (Physics.Raycast(ray, out hit, 100, mask)) {
            // Tracking for placement guideline
            m_NebulaTracking.transform.position = new Vector3(hit.point.x, -0.3f, hit.point.z);

            // Left click to place nebula
            if (Input.GetMouseButtonDown(0)) {
                CreateNebula(hit.point);
            }

            // Right click to remove nebula
            if (Input.GetMouseButtonDown(1)) {
                Debug.Log(hit.collider.gameObject.layer);
                if (hit.collider.gameObject.layer == 11) {
                    Debug.Log("hit on right click");
                    DeleteNebula(hit.collider.gameObject);
                }
            }
        }
    }

    //  Creates a StarSystem object
    void CreateSystem(Vector3 syspos) {

        // Create visual object
        GameObject.Instantiate(sysprefab, syspos, transform.rotation);
        
        // Create StarSystem object
        systems.Add(new StarSystem(new Vector2(syspos.x, syspos.z)));
    }

    // Deletes a StarSystem object
    void DeleteSystem(Vector3 syspos, GameObject sysobj) {
        // Delete visual object
        Destroy(sysobj);

        // Remove StarSystem object from list
        Vector2 npos = new Vector2(syspos.x, syspos.z); 
        for (int i = 0; i < systems.Count; i++) {
            if (systems[i].GetPos() == npos) {
                systems.RemoveAt(i);
                return;
            }
        }
    }

    //  Creates a Hyperlane object
    void CreateLane(Vector2 npos, StarSystem nsys) {
        // Add Hyperlane object to list
        hyperlanes.Add(new Hyperlane(start_point, nsys));

        // Create visual object
        GameObject l = GameObject.Instantiate(lane_obj, new Vector3(start_point.GetPos().x + ((npos.x - start_point.GetPos().x) / 2),0,start_point.GetPos().y + ((npos.y - start_point.GetPos().y) / 2)), new Quaternion());
        l.transform.localScale = new Vector3(0.2f,0.2f,Vector2.Distance(npos, start_point.GetPos()));
                            
        l.transform.LookAt(new Vector3(npos.x,0,npos.y),Vector3.right);

        l.GetComponent<LineRenderer>().SetPosition(0, new Vector3(start_point.GetPos().x, 0, start_point.GetPos().y));
        l.GetComponent<LineRenderer>().SetPosition(1, new Vector3(npos.x, 0, npos.y));
        Mesh m = new Mesh();
        l.GetComponent<LineRenderer>().BakeMesh(m, false);
        MeshCollider mc = l.AddComponent<MeshCollider>();
        mc.sharedMesh = m;
        laneplace_primed = false;

        return;
    }

    // Deletes a Hyperlane object
    void DeleteLane(GameObject laneobj) {

        Vector3 s = laneobj.GetComponent<LineRenderer>().GetPosition(0);
        Vector3 t = laneobj.GetComponent<LineRenderer>().GetPosition(1);

        Vector2 s_pos = new Vector2(s.x, s.z);
        Vector2 t_pos = new Vector2(t.x, t.z);

        for (int i = 0; i < hyperlanes.Count; i++) {
            if (s_pos == hyperlanes[i].GetStart() && t_pos == hyperlanes[i].GetEnd()) {
                hyperlanes.RemoveAt(i);
                break;
            }
        }

        // Delete visual object
        Destroy(laneobj);
    }

    void CreateNebula(Vector3 nebpos) {
        // Create visual object
        GameObject neb = GameObject.Instantiate(nebprefab, new Vector3(nebpos.x, -0.3f, nebpos.z), transform.rotation);
        neb.transform.localScale = m_NebulaTracking.transform.localScale;
        neb.transform.eulerAngles = new Vector3(0f, 0f, 0f);

        // Create Nebula object
        nebulae.Add(new Nebula(new Vector2(nebpos.x, nebpos.z), m_NebulaTracking.transform.localScale.x));
    }
    void DeleteNebula(GameObject nebobj) {

        // Remove nebula from list
        Vector2 npos = new Vector2(nebobj.transform.position.x, nebobj.transform.position.z);
        for (int i = 0; i < nebulae.Count; i++) {
            if (nebulae[i].GetPos() == npos) {
                nebulae.RemoveAt(i);
                break;
            }
        }

        // Destroy Visial Object
        Destroy(nebobj);
    }

    #endregion

    #region Map Settings

    public void SetMapName() {
        string nname = GameObject.Find("NameField").transform.GetChild(0).GetComponent<InputField>().text;
        map_name = nname;
    }

    public void SetMapPriority() {
        int npriority = int.Parse(GameObject.Find("PriorityField").transform.GetChild(0).GetComponent<InputField>().text);
        map_priority = npriority;
     }

    public void SetMapDefault() {
        bool ndefault = GameObject.Find("DefaultToggleLabel").GetComponent<Toggle>().isOn;
        is_default = ndefault;
    }
    public void SetMinEmpires(int nme) {
        min_empires = nme;
    }
    public void SetMaxEmpires(int nme) {
        max_empires = nme;
    }
    public void SetDefaultEmpireNumbers(int ndfn) {
        num_empire_default = ndfn;
    }
    public void SetDefaulFENumbers(int ndfen) {
        fallen_empire_default = ndfen;
    }
    public void SetFallenEmpireMax(int nfem) {
        fallen_empire_max = nfem;
    }
    public void SetDefaultMaxEmpires(int naed) {
        advanced_empire_default = naed;
    }
    public void SetColonizablePlanetOdds(float nodds) {
        colonizable_planet_odds = nodds;
    }
    public void SetRandomHyperlanes(bool nr) {
        random_hyperlanes = nr;
    }
    public void SetCoreRadius(int ncr) {
        core_radius = ncr;
    }

    #endregion

    public void CloseEditUI() {
        edit_ui.SetActive(false);
        open_edit_ui.SetActive(true);
    }

    public void OpenEditUI() {
        edit_ui.SetActive(true);
        open_edit_ui.SetActive(false);
        GameObject.Find("CameraController").GetComponent<CameraMotion>().FetchInputFields();    // Update camera's array of InputFields
    }

    public void ExitConfirmationPopup() {
        quit_editor_popup.SetActive(true);
    }

    public void ExitEditor() {
        Application.Quit();
    }

    public void CancelExit() {
        quit_editor_popup.SetActive(false);
    }

    // Change edit mode
    public void ChangeMode(int m) {
        mode = m;

        // Change button colors
        ColorBlock hcol = mode_buttons[0].colors;
        hcol.normalColor = hcol.selectedColor;
        ColorBlock ncol = mode_buttons[0].colors;
        ncol.normalColor = button_normal_color;
        for (int i = 0; i < mode_buttons.Length; i++) {
            if (m == i) {
                //Debug.LogFormat("Highlighted color {0}", i);
                mode_buttons[i].colors = hcol;
            }
            else {
                mode_buttons[i].colors = ncol;
            }
        }
    }

    public void ImportMap(string fileName) {

        // Clear exisitng objects
        GameObject[] mapobjs = GameObject.FindGameObjectsWithTag("Map Object");
        for (int i = 0; i < mapobjs.Length; i++) {
            Destroy(mapobjs[i]);
        }
        systems.Clear();
        hyperlanes.Clear();
        nebulae.Clear();

        FileInfo sourceFile = null;
        StreamReader reader = null;
        string lineText = "";

        sourceFile = new FileInfo(fileName);
        reader = sourceFile.OpenText();

        List<StarSystem> nsystems = new List<StarSystem>();
        List<Vector2> nlanes = new List<Vector2>();

        while (lineText != null) {
            lineText = reader.ReadLine();
            if (lineText == null) { break; }
            char[] separators = new char[] {' ','\t','{','}','=','"'};
            string[] words = lineText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Settings
            if (words.Length > 0) {

                // Galaxy Settings
                if (words[0] == "name") { 
                    GameObject.Find("GalaxySettings/NameField/InputField").GetComponent<InputField>().text = words[1]; 
                }
                if (words[0] == "priority") { 
                    GameObject.Find("GalaxySettings/PriorityField/InputField").GetComponent<InputField>().text = words[1]; 
                }
                if (words[0] == "default") { 
                    if (words[1] == "yes") {
                        GameObject.Find("GalaxySettings/DefaultCheckField/DefaultToggleLabel").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                    }
                    if (words[1] == "no") {
                        GameObject.Find("GalaxySettings/DefaultCheckField/DefaultToggleLabel").GetComponent<Toggle>().SetIsOnWithoutNotify(false);
                    }
                }

                // Empire Settings
                if (words[0] == "num_empires") {
                    GameObject.Find("EmpireSettings/Standard Empires/EmpireRandomization/MinEmpires").GetComponent<InputField>().text = words[2];
                    GameObject.Find("EmpireSettings/Standard Empires/EmpireRandomization/MaxEmpires").GetComponent<InputField>().text = words[4];
                }
                if (words[0] == "num_empire_default") {
                    GameObject.Find("EmpireSettings/Standard Empires/NumEmpiresDef/InputField").GetComponent<InputField>().text = words[1];
                }
                if (words[0] == "advanced_empires") {
                    GameObject.Find("EmpireSettings/Advanced Empires/EmpireRandomization/MinEmpires").GetComponent<InputField>().text = words[2];
                    GameObject.Find("EmpireSettings/Advanced Empires/EmpireRandomization/MaxEmpires").GetComponent<InputField>().text = words[4];
                }
                if (words[0] == "advanced_empire_default") {
                    GameObject.Find("EmpireSettings/Advanced Empires/NumAdvEmpiresDef/InputField").GetComponent<InputField>().text = words[1];
                }
                if (words[0] == "fallen_empires") {
                    GameObject.Find("EmpireSettings/Fallen Empires/EmpireRandomization/MinEmpires").GetComponent<InputField>().text = words[2];
                    GameObject.Find("EmpireSettings/Fallen Empires/EmpireRandomization/MaxEmpires").GetComponent<InputField>().text = words[4];
                }
                if (words[0] == "fallen_empire_default") {
                    GameObject.Find("EmpireSettings/Fallen Empires/NumFalEmpiresDef/InputField").GetComponent<InputField>().text = words[1];
                }

                // Geography Settings
                if (words[0] == "colonizable_planet_odds") {
                    GameObject.Find("GeographySettings/ColonizFreq/InputField").GetComponent<InputField>().text = words[1];
                }
                if (words[0] == "num_hyperlanes") {
                    GameObject.Find("GeographySettings/RandomHyperlanes/HyperlaneRandomization/MinLanes").GetComponent<InputField>().text = words[2];
                    GameObject.Find("GeographySettings/RandomHyperlanes/HyperlaneRandomization/MaxLanes").GetComponent<InputField>().text = words[4];
                }
                if (words[0] == "num_hyperlanes_default") {
                    GameObject.Find("GeographySettings/RandomHyperlanes/NumLanesDef/InputField").GetComponent<InputField>().text = words[1];
                }
                if (words[0] == "random_hyperlanes") { 
                    if (words[1] == "yes") {
                        GameObject.Find("GeographySettings/RandomHyperlanes/DefaultToggleLabel").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                    }
                    if (words[1] == "no") {
                        GameObject.Find("GeographySettings/RandomHyperlanes/DefaultToggleLabel").GetComponent<Toggle>().SetIsOnWithoutNotify(false);
                    }
                }
            }

            // Systems
            if (lineText.Contains("system = {")) {

                
                string name = "null_sysname";
                string init = "null_sysinit";
                Vector2 pos = new Vector2();
                int id = -1;

                for (int i = 0; i < words.Length; i++) {
                    //Debug.Log(words[i]);
                    // Position
                    if (words[i] == "position") {
                        //Debug.LogFormat("position {0},{1}",words[i+2],words[i+4]);
                        pos = new Vector2((float)int.Parse(words[i+2]) / -20, (float)int.Parse(words[i+4]) / -20);
                    }
                    // Name
                    if (words[i] == "name") {
                        Debug.LogFormat("name = {0}", words[i+1]);
                        name = words[i+1];
                    }
                    // Initializer
                    if (words[i] == "initializer") {
                        //Debug.LogFormat("initializer = {0}", words[i+1]);
                        init = words[i+1];
                    }
                    // ID
                    if (words[i] == "id") {
                        //Debug.LogFormat("id = {0}", words[i+1]);
                        id = int.Parse(words[i+1]);
                    }
                }
                StarSystem nsys = new StarSystem(pos);
                nsys.SetID(id);
                if (name != "null_sysname") {nsys.SetName(name);}
                if (init != "null_sysinit") {nsys.SetInitializer(init);}
                Debug.Log(nsys.GetName());
                nsystems.Add(nsys);
            }

            // Hyperlanes
            if (lineText.Contains("add_hyperlane = {")) { 
                nlanes.Add(new Vector2(int.Parse(words[2]),int.Parse(words[4])));
            } 

            // Nebulae
            if (lineText.Contains("nebula = {")) { 
                Vector2 nebpos = new Vector2((float)int.Parse(words[3]) / 20,(float)int.Parse(words[5]) / 20);
                float rad = int.Parse(words[7]) / 10;
                        
                // Create visual object
                GameObject neb = GameObject.Instantiate(nebprefab, new Vector3(nebpos.x, -0.3f, nebpos.y), transform.rotation);
                neb.transform.localScale = new Vector3(rad, 0.1f, rad);
                neb.transform.eulerAngles = new Vector3(0f, 0f, 0f);

                // Create Nebula object
                nebulae.Add(new Nebula(new Vector2(nebpos.x, nebpos.y), rad));
            
            } 
        }

        // Instantiate systems
        for (int i = 0; i < nsystems.Count; i++) {
            // Find system in nsystems such that the system's id == i
            int sysid = -1;
            for (int j = 0; j < nsystems.Count; j++) {
                if (i == nsystems[j].GetID()) {
                    sysid = j;
                    break;
                }
            } 
            // Create the 3D coordinate vector at which to instantiate the model
            Vector3 syspos = new Vector3(nsystems[sysid].GetPos().x, 0, nsystems[sysid].GetPos().y);

            // Create visual object
            GameObject clone = GameObject.Instantiate(sysprefab, syspos, transform.rotation);
            if (nsystems[sysid].GetName() != null) {
                clone.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = nsystems[sysid].GetName();
            }

            // Add the system to systems
            systems.Add(nsystems[sysid]);
        }

        // Instantiate Lanes
        for (int i = 0; i < nlanes.Count; i++) {
            start_point = systems[(int)nlanes[i].x];
            CreateLane(systems[(int)nlanes[i].y].GetPos(), systems[(int)nlanes[i].y]);
        }
    }

    // Export map
    public void ExportMap(string fileName) {

        List<string> lines = new List<string>();
        lines.Add("static_galaxy_scenario = {");

        List<string> headerStrings = new List<string>(); 
        headerStrings.Add("\tname = \"" + GameObject.Find("GalaxySettings/NameField/InputField/Text").GetComponent<Text>().text + "\"");
        headerStrings.Add("\tpriority = " + GameObject.Find("GalaxySettings/PriorityField/InputField/Text").GetComponent<Text>().text );
        if (GameObject.Find("GalaxySettings/DefaultCheckField/DefaultToggleLabel").GetComponent<Toggle>().isOn) { headerStrings.Add("\tdefault = yes"); } else { headerStrings.Add("\tdefault = no"); }
        
        // Standard Empire Numbers
        headerStrings.Add("\tnum_empires = { min = " + GameObject.Find("EmpireSettings/Standard Empires/EmpireRandomization/MinEmpires/Text").GetComponent<Text>().text + " max = " + GameObject.Find("EmpireSettings/Standard Empires/EmpireRandomization/MaxEmpires/Text").GetComponent<Text>().text + " }" );
        headerStrings.Add("\tnum_empire_default = " + GameObject.Find("EmpireSettings/Standard Empires/NumEmpiresDef/InputField/Text").GetComponent<Text>().text );

        // Advanced Empire Numbers
        headerStrings.Add("\tadvanced_empires = { min = " + GameObject.Find("EmpireSettings/Advanced Empires/EmpireRandomization/MinEmpires/Text").GetComponent<Text>().text + " max = " + GameObject.Find("EmpireSettings/Advanced Empires/EmpireRandomization/MaxEmpires/Text").GetComponent<Text>().text + " }" );
        headerStrings.Add("\tadvanced_empire_default = " + GameObject.Find("EmpireSettings/Advanced Empires/NumAdvEmpiresDef/InputField/Text").GetComponent<Text>().text );

        // Fallen Empire Numbers
        headerStrings.Add("\tfallen_empires = { min = " + GameObject.Find("EmpireSettings/Fallen Empires/EmpireRandomization/MinEmpires/Text").GetComponent<Text>().text + " max = " + GameObject.Find("EmpireSettings/Fallen Empires/EmpireRandomization/MaxEmpires/Text").GetComponent<Text>().text + " }" );
        headerStrings.Add("\tfallen_empire_default = " + GameObject.Find("EmpireSettings/Fallen Empires/NumFalEmpiresDef/InputField/Text").GetComponent<Text>().text );

        // Habitable Planet Frequency
        headerStrings.Add("\tcolonizable_planet_odds = " + GameObject.Find("GeographySettings/ColonizFreq/InputField/Text").GetComponent<Text>().text );

        // Hyperlane Randomization
        headerStrings.Add("\tnum_hyperlanes = { min = " + GameObject.Find("GeographySettings/RandomHyperlanes/HyperlaneRandomization/MinLanes/Text").GetComponent<Text>().text + " max = " + GameObject.Find("GeographySettings/RandomHyperlanes/HyperlaneRandomization/MaxLanes/Text").GetComponent<Text>().text + " }" );
        headerStrings.Add("\tnum_hyperlanes_default = " + GameObject.Find("GeographySettings/RandomHyperlanes/NumLanesDef/InputField/Text").GetComponent<Text>().text );
        if (GameObject.Find("GeographySettings/RandomHyperlanes/DefaultToggleLabel").GetComponent<Toggle>().isOn) { headerStrings.Add("\trandom_hyperlanes = yes"); } else { headerStrings.Add("\trandom_hyperlanes = no"); }
        
        lines.AddRange(headerStrings);

        lines.Add("\n\t#Systems");
        List<string> systemsStrings = new List<string>();
        for (int i = 0; i < systems.Count; i++) {
            string sysLine = "\tsystem = {";
            sysLine += " id = \"" + i + "\"";
            if (systems[i].GetName() != null) { sysLine += " name = \"" + systems[i].GetName() + "\""; }
            sysLine += " position = { x = " + (int)(systems[i].GetPos().x * -20) + " y = " + (int)(systems[i].GetPos().y * -20) + " }";
            // initializers
            if (systems[i].GetInitializer() != null && systems[i].GetInitializer().Length > 0) { sysLine += " initializer = " + systems[i].GetInitializer() + " "; }
            // spawnweight
            sysLine += "}";
            Debug.Log(sysLine);
            systemsStrings.Add(sysLine);
        }
        lines.AddRange(systemsStrings);

        lines.Add("\n\t#Hyperlanes");
        List<string> hyperlaneStrings = new List<string>();
        for (int i = 0; i < hyperlanes.Count; i++) {
            string laneline = "\tadd_hyperlane = { from = \"";
            int sid = -1;
            for (int j = 0; j < systems.Count; j++) {
                if (hyperlanes[i].GetStart() == systems[j].GetPos()) {
                    sid = j;
                    break;
                }
            }
            int eid = -1;
            for (int j = 0; j < systems.Count; j++) {
                if (hyperlanes[i].GetEnd() == systems[j].GetPos()) {
                    eid = j;
                    break;
                }
            }
            laneline += sid.ToString() + "\" to = \"" + eid.ToString() + "\" }";
            Debug.Log(laneline);
            hyperlaneStrings.Add(laneline); 
        }
        lines.AddRange(hyperlaneStrings);

        lines.Add("\n\t#Nebulae");
        List<string> nebulaeStrings = new List<string>();
        for (int i = 0; i < nebulae.Count; i++) {
            string nebLine = "\tnebula = {";
            // name
            nebLine += " position = { x = " + (int)(nebulae[i].GetPos().x * 20) + " y = " + (int)(nebulae[i].GetPos().y * 20) + " }";
            nebLine += " radius = " + nebulae[i].GetRadius();
            nebLine += "}";
            Debug.Log(nebLine);
            nebulaeStrings.Add(nebLine);
        }
        lines.AddRange(nebulaeStrings);

        string endFile = "}";
        lines.Add(endFile);

        System.IO.File.Delete(fileName);
        System.IO.File.AppendAllLines(fileName, lines);

    }
}
