using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeMenuManager : MonoBehaviour
{
    //Public References
    public GameObject EscapeMenu;
    
    //Private References
    private FlowFieldControls _controls;
    private bool _escapeMenu = false;
    
    private void Awake()
    {
        _controls = new FlowFieldControls();
    }
    
    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void Update()
    {
        ReadInput();
        ShowEscapeMenu();
    }

    private void ReadInput()
    {
        _escapeMenu = (_controls.UI.EscapeMenu.triggered) ? !_escapeMenu : _escapeMenu;
    }
    
    private void ShowEscapeMenu()
    {
        EscapeMenu.SetActive(_escapeMenu);
    }

    #region Button Functions

    public void SwitchToBoidsSim()
    {
        SceneManager.LoadScene(0);
    }
    
    public void SwitchToVoxelSim()
    {
        SceneManager.LoadScene(1);
    }
    
    public void SwitchToFlowFieldSim()
    {
        SceneManager.LoadScene(2);
    }

    public void ExitSim()
    {
        Application.Quit();
    }

    #endregion
}
