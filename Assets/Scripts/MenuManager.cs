using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject[] menus; //will hold references to all menu GameObjects
    public GameObject initalMenu; //the menu that should be active at the start

    void Start()
    {
        initalMenu.SetActive(true); // Activate the initial menu
        foreach (GameObject menu in menus)
        {
            if (menu != initalMenu)
            {
                menu.SetActive(false); // Deactivate all other menus
            }
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
