using System;
using UnityEngine;
using UnityEngine.UI;
using static Boids3DController;

public class ValueDisplay : MonoBehaviour
{
    //Constants
    private const int Maxboids = 67000000;
    
    //Public References
    [Header("References")]
    public Boids3DController BoidController;
    public GameObject Canvas;
    [Header("Fields")]
    public Slider BoidSizeSlider;
    public Text BoidSizeText;
    public Slider ViewRadiusSlider;
    public Text ViewRadiusText;
    public Slider AvoidRadiusSlider;
    public Text AvoidRadiusText;
    public Slider AlignmentSlider;
    public Text AlignmentText;
    public Slider CohesionSlider;
    public Text CohesionText;
    public Slider SeparationSlider;
    public Text SeparationText;
    public Slider CentrePullSlider;
    public Text CentrePullText;
    public Slider SpeedSlider;
    public Text SpeedText;
    public InputField BoidAmountField;
    public Text BoidAmountFieldTextComponent;
    [Header("Boid Amount Feedback")] 
    public Color StandardColour;
    public int AdvisoryLimit;
    public Color AdvisoryColour;
    public int WarningLimit;
    public Color WarningColour;
    
    //Limits
    private const int AvoidRadiusMax = 5;
    
    //Private References
    private Boid3DControls _controls;
    private bool _hideUI = false;
    private float _boidSize;
    private int _viewRadius;
    private int _avoidRadius;
    private float _alignment;
    private float _cohesion;
    private float _separation;
    private float _centrePull;
    private float _speed;

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
        HideUI();
        GetValues();
        LimitValues();
        SetValues();
        UpdateTexts();
        BoidAmountFeedback();
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        _hideUI = (_controls.UI.HideUI.triggered) ? !_hideUI : _hideUI;
    }

    private void HideUI()
    {
        Canvas.SetActive(!_hideUI);
    }

    #endregion
    
    private void GetValues()
    {
        //Get Values From Sliders
        _boidSize = BoidSizeSlider.value;
        _viewRadius = (int) ViewRadiusSlider.value;
        _avoidRadius = (int) AvoidRadiusSlider.value;
        _alignment = AlignmentSlider.value;
        _cohesion = CohesionSlider.value;
        _separation = SeparationSlider.value;
        _centrePull = CentrePullSlider.value;
        _speed = SpeedSlider.value;
    }

    private void LimitValues()
    {
        //Certain Values Require Limitation E.g Avoid !> View
        //Avoid Radius
        int currentAvoidRadius = _avoidRadius;
        AvoidRadiusSlider.maxValue = Mathf.Min(ViewRadiusSlider.value, AvoidRadiusMax);
        if (AvoidRadiusSlider.maxValue < currentAvoidRadius)
        {
            _avoidRadius = (int) AvoidRadiusSlider.maxValue;
            SetValues();
        }
    }
    
    private void UpdateTexts()
    {
        //Boid Size
        BoidSizeText.text = $"{_boidSize}";
        
        //View Radius
        ViewRadiusText.text = $"{_viewRadius}";
        
        //Avoid Radius
        AvoidRadiusText.text = $"{_avoidRadius}";
        
        //Alignment
        AlignmentText.text = $"{_alignment}";
        
        //Cohesion
        CohesionText.text = $"{_cohesion}";
        
        //Separation
        SeparationText.text = $"{_separation}";
        
        //Centre Pull
        CentrePullText.text = $"{_centrePull}";
        
        //Speed
        SpeedText.text = $"{_speed}";
    }

    private void BoidAmountFeedback()
    {
        //Get Boid Amount
        string boidAmountFieldText = BoidAmountField.text;
        if (boidAmountFieldText != "")
        {
            int boidAmount = Int32.Parse(boidAmountFieldText);

            if (boidAmount > WarningLimit)
            {
                BoidAmountFieldTextComponent.color = WarningColour;
            }
            else if (boidAmount > AdvisoryLimit)
            {
                BoidAmountFieldTextComponent.color = AdvisoryColour;
            }
            else
            {
                BoidAmountFieldTextComponent.color = StandardColour;
            }
        }
    }

    public void SetValues()
    {
        SimulationValues values = new SimulationValues()
        {
            BoidSize = _boidSize,
            ViewRadius = _viewRadius,
            AvoidRadius = _avoidRadius,
            Alignment = _alignment,
            Cohesion = _cohesion,
            Separation = _separation,
            CentrePull = _centrePull,
            Speed = _speed
        };
        
        //Set Values
        BoidController.SetValues(values);
    }

    public void RespawnBoids()
    {
        string boidAmountFieldText = BoidAmountField.text;
        if (boidAmountFieldText != "")
        {
            int boidAmount = Int32.Parse(boidAmountFieldText);
            
            //Limit To 67 Million (Max Thread Group Count * 1024)
            if (boidAmount > Maxboids)
            {
                boidAmount = Maxboids;
                BoidAmountField.text = $"{Maxboids}";
            }
            
            BoidController.RespawnBoids(boidAmount);
        }
    }
}
