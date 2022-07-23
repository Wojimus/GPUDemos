using Unity.Mathematics;
using UnityEngine;

public class CameraLook : MonoBehaviour
{
    #region Public Variables
    
    [Header("Speed")]
    public float ZoomSpeed;
    public float ZoomSmoothing;
    public float PanSpeed;
    public float PanSmoothing;
    public float RotationSpeed;
    public float RotationSmoothing;
    public float ShiftMultiplier;

    [Header("Boundaries")] 
    public float2 ZoomBoundaries;
    public float2 PanBoundaries;
    
    #endregion

    #region Private Variables

    //References
    private VoxelsAndGrassControls _playerControls;

    //Position
    private float _currentZoom;
    private float _desiredZoom;
    private float2 _currentPosition;
    private float2 _desiredPosition;
    
    //Rotation
    private float _currentRotation;
    private float _desiredRotation;
    
    //Input
    private float _xAxis;
    private float _yAxis;
    private float _rotate;
    private float _scroll;
    private bool _shift;

    #endregion

    #region Unity Functions

    private void Awake()
    {
        _playerControls = new VoxelsAndGrassControls();
    }

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }

    private void Update()
    {
        ReadInput();
        Panning();
        Rotate();
        Zoom();
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        //Scroll
        _scroll = _playerControls.Camera.Zoom.ReadValue<float>() * -1;
        _scroll = Mathf.Clamp(_scroll, -1, 1);
        
        //Panning
        _xAxis = _playerControls.Camera.XAxis.ReadValue<float>();
        _yAxis = _playerControls.Camera.YAxis.ReadValue<float>();
        
        //Rotation
        _rotate = _playerControls.Camera.Rotate.ReadValue<float>();
        
        //Shift
        _shift = (int)_playerControls.Camera.Shift.ReadValue<float>() == 1;
    }

    #endregion

    #region Movement

    private void Panning()
    {
        //Create Velocity
        Vector3 velocity = new Vector3(_xAxis, 0, _yAxis).normalized;
        
        //Convert Velocity To Transform Direction
        velocity = transform.TransformDirection(velocity);

        //Shift Multiplier
        if (_shift) velocity *= ShiftMultiplier;
        
        //Create New Position
        _desiredPosition.x += velocity.x * Time.unscaledDeltaTime * PanSpeed;
        _desiredPosition.y += velocity.z * Time.unscaledDeltaTime * PanSpeed;

        //Clamp Position
        _desiredPosition.x = Mathf.Clamp(_desiredPosition.x, PanBoundaries.x, PanBoundaries.y);
        _desiredPosition.y = Mathf.Clamp(_desiredPosition.y, PanBoundaries.x, PanBoundaries.y);

        //Lerp To New Position
        _currentPosition.x = Mathf.Lerp(_currentPosition.x, _desiredPosition.x, PanSmoothing * Time.unscaledDeltaTime);
        _currentPosition.y = Mathf.Lerp(_currentPosition.y, _desiredPosition.y, PanSmoothing * Time.unscaledDeltaTime);

        //Set New Position
        Vector3 newPosition = new Vector3(_currentPosition.x, transform.position.y, _currentPosition.y);
        transform.position = newPosition;
    }

    private void Rotate()
    {
        //Shift Multiplier
        float shiftMultiplier = _shift ? ShiftMultiplier : 1;
        
        //Create New Rotation
        _desiredRotation += _rotate * Time.unscaledDeltaTime * RotationSpeed * shiftMultiplier;

        //Lerp To New Rotation
        _currentRotation = Mathf.Lerp(_currentRotation, _desiredRotation, RotationSmoothing * Time.unscaledDeltaTime);
        
        //Set Rotation
        Vector3 newRotation = new Vector3(transform.rotation.eulerAngles.x, _currentRotation, transform.rotation.eulerAngles.z);

        transform.rotation = Quaternion.Euler(newRotation);
    }

    private void Zoom()
    {
        //Shift Multiplier
        float shiftMultiplier = _shift ? ShiftMultiplier : 1;
        
        //Create New Zoom
        _desiredZoom += _scroll * Time.unscaledDeltaTime * ZoomSpeed * shiftMultiplier;

        //Clamp Zoom
        _desiredZoom = Mathf.Clamp(_desiredZoom, ZoomBoundaries.x, ZoomBoundaries.y);
        
        //Lerp Zoom
        _currentZoom = Mathf.Lerp(_currentZoom, _desiredZoom, ZoomSmoothing * Time.unscaledDeltaTime);

        //Set New Zoom
        transform.position = new Vector3(transform.position.x, _currentZoom, transform.position.z);
    }

    #endregion
}
