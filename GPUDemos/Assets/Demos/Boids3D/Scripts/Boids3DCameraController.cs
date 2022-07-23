using System;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class Boids3DCameraController : MonoBehaviour
{
    //Public References
    public GameObject FieldObject;
    public float PanSpeed = 1f;
    public float PanShiftMultiplier = 2;
    public float ZoomSpeed = 1f;
    public float ZoomShiftMultiplier = 2;
    public Vector2 ZoomLimits;
    
    //Private References
    private Boid3DControls _controls;
    private float _yAxis;
    private float _xAxis;
    private float _rotate;
    private float _zoom;
    private bool _shift;

    #region Unity Functions

    private void Awake()
    {
        _controls = new Boid3DControls();
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }
    
    void Update()
    {
        ReadInput();
        Panning();
        Zoom();
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        //Panning
        _yAxis = _controls.Camera.PanY.ReadValue<float>();
        _xAxis = _controls.Camera.PanX.ReadValue<float>();
        _rotate = _controls.Camera.Rotate.ReadValue<float>();
        
        //Zoom
        _zoom = Mathf.Clamp(_controls.Camera.Zoom.ReadValue<float>(), -1, 1);
        
        //Shift
        _shift = (int)_controls.Camera.Shift.ReadValue<float>() == 1;
    }

    #endregion

    #region Movement

    private void Panning()
    {
        //Calculate Degrees To Rotate
        float rotationY = _yAxis * PanSpeed * Time.deltaTime;
        float rotationX = _xAxis * PanSpeed * Time.deltaTime * -1; //Invert X
        float rotate = _rotate * PanSpeed * Time.deltaTime * -1; //Invert Rotation
        
        //Shift Multiplier
        rotationY *= _shift ? PanShiftMultiplier : 1;
        rotationX *= _shift ? PanShiftMultiplier : 1;
        rotate *= _shift ? PanShiftMultiplier : 1;

        //Pan
        transform.RotateAround(FieldObject.transform.position, transform.right, rotationY);
        transform.RotateAround(FieldObject.transform.position, transform.up, rotationX);
        
        //Rotate
        transform.RotateAround(FieldObject.transform.position, transform.forward, rotate);
    }

    private void Zoom()
    {
        //Clamp Limits
        //Get Distance To Centre
        float distanceToCentre = Vector3.Distance(transform.position, FieldObject.transform.position);
        if (distanceToCentre < ZoomLimits.x) _zoom = Mathf.Clamp(_zoom, -1, 0);
        else if (distanceToCentre > ZoomLimits.y) _zoom = Mathf.Clamp(_zoom, 0, 1);

        //Calculate Velocity
        float zoomVelocity = _zoom * ZoomSpeed * Time.deltaTime;
        
        //Shift Multiplier
        zoomVelocity *= _shift ? ZoomShiftMultiplier : 1;

        //Set Position
        transform.position = Vector3.MoveTowards(transform.position, FieldObject.transform.position, zoomVelocity);
    }

    #endregion
    
}
