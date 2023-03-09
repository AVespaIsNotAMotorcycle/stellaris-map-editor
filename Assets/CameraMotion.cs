using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraMotion : MonoBehaviour
{
    public float movespeed = 6f;
    public float scrollspeed = 6f;
    private InputField[] inputFields = {};
    // UI Raycasting
    public Canvas m_canvas;
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    // Collect array of all input fields
    // This is called by other objects whenever the UI is refreshed
    // Wouldn't want it running in Update(), as it's expensive
    public void FetchInputFields() 
    {
        inputFields = FindObjectsOfType<InputField>();
    }

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

        // Check if user is typing
        // If so, don't move
        if (inputFields.Length > 0) {
            for (int i = 0; i < inputFields.Length; i++) {
                if (inputFields[i].isFocused) { return; }
            }
        }
        float heightmod = transform.position[1] / 4 + 1;
        float deltax = Input.GetAxis("Horizontal") * movespeed * Time.deltaTime * heightmod;
        float deltay = Input.GetAxis("Vertical") * movespeed * Time.deltaTime * heightmod;
        float deltaz = Input.GetAxis("Mouse ScrollWheel") * scrollspeed * Time.deltaTime * heightmod;
        if (Input.GetKey("left ctrl")) { deltaz = 0.0f; };
        if (deltaz < 0 && heightmod <= 1) { deltaz = 0.0f; };
        if (deltaz > 0 && heightmod >= 20) { deltaz = 0.0f; };
        if (results.Count != 0) { deltaz = 0.0f; }; 

        transform.Translate(deltax, deltaz, deltay);
    }
}
