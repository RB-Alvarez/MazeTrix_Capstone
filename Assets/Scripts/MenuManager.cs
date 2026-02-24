using System.Collections.Generic;
using UnityEngine;

// Menu refers to a group of UI elements
// Canvas refers to the parent GameObject that holds multiple Menus

public class MenuManager : MonoBehaviour
{
    public GameObject[] cavases; //will hold references to all canvas GameObjects, if needed for additional functionality
    public GameObject initialCanvas; //the canvas that should be active at the start, if needed for additional functionality
    public GameObject[] menus; //will hold references to all menu GameObjects
    public GameObject initialMenu; //the menu that should be active at the start
    

    void Start()
    {
        initialCanvas.SetActive(true); // Activate the initial canvas
        foreach (GameObject canvas in cavases)
        {
            if (canvas != initialCanvas)
            {
                canvas.SetActive(false); // Deactivate all other canvases
            }
        }

        initialMenu.SetActive(true); // Activate the initial menu
        foreach (GameObject menu in menus)
        {
            if (menu != initialMenu)
            {
                menu.SetActive(false); // Deactivate all other menus
            }
        }
    }

    // Open a canvas by activating it and deactivating all others
    public void OpenCanvas(GameObject openThisCanvas)
    {
        if (openThisCanvas == null)
        {
            return;
        }
        foreach (GameObject canvas in cavases)
        {
            canvas.SetActive(canvas == openThisCanvas); // Activate only the selected canvas
        }
    }

    // Open a menu by activating it and deactivating all others
    public void OpenSingleMenu(GameObject openThisMenu)
    {
        OpenMenu(openThisMenu, false);
    }

    // Open a menu additively without closing others, useful for shared elements like a pause menu or inventory.
    public void OpenAdditionalMenu(GameObject openThisMenu)
    {
        OpenMenu(openThisMenu, true);
    }

    public void OpenMenu(GameObject openThisMenu, bool additive)
    {
        if (openThisMenu == null)
        {
            return;
        }

        if (additive)
        {
            openThisMenu.SetActive(true);
            return;
        }

        foreach (GameObject menu in menus)
        {
            menu.SetActive(menu == openThisMenu); // Activate only the selected menu
        }
    }

}
