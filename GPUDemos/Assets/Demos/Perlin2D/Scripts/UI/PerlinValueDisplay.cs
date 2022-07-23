using System;
using UnityEngine;
using UnityEngine.UI;

public class PerlinValueDisplay : MonoBehaviour
{
    //Constants
    private const int MaxParticles = 67000000;
    
    //Public References
    [Header("References")]
    public FlowFieldController FlowFieldController;
    public GameObject Canvas;
    [Header("Fields")]
    public Slider FieldScaleSlider;
    public Text FieldScaleText;
    public Slider FieldStrengthSlider;
    public Text FieldStrengthText;
    public Slider FieldSpeedSlider;
    public Text FieldSpeedText;
    public Slider ParticleSpeedSlider;
    public Text ParticleSpeedText;
    public InputField ParticleAmountField;
    public Text ParticleAmountFieldTextComponent;
    [Header("Particle Amount Feedback")] 
    public Color StandardColour;
    public int AdvisoryLimit;
    public Color AdvisoryColour;
    public int WarningLimit;
    public Color WarningColour;
    
    //Private References
    private FlowFieldControls _controls;
    private bool _hideUI = false;
    private float _fieldScale;
    private float _fieldStrength;
    private float _fieldSpeed;
    private float _particleSpeed;
    
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
    
    void Update()
    {
        ReadInput();
        HideUI();
        GetValues();
        UpdateTexts();
        SetValues();
        ParticleAmountFeedback();
    }
    
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
        _fieldScale = FieldScaleSlider.value;
        _fieldStrength = FieldStrengthSlider.value;
        _fieldSpeed = FieldSpeedSlider.value;
        _particleSpeed = ParticleSpeedSlider.value;
    }

    private void SetValues()
    {
        FlowFieldController.FlowFieldScale = _fieldScale;
        FlowFieldController.FlowFieldStrength = _fieldStrength;
        FlowFieldController.FlowFieldSpeed = _fieldSpeed;
        FlowFieldController.ParticleSpeed = _particleSpeed;
    }
    
    private void UpdateTexts()
    {
        //Field Scale
        FieldScaleText.text = $"{(_fieldScale)}";
        
        //Field Strength
        FieldStrengthText.text = $"{(_fieldStrength)}";
        
        //Field Speed
        FieldSpeedText.text = $"{_fieldSpeed}";
        
        //Particle Speed
        ParticleSpeedText.text = $"{_particleSpeed}";
    }
    
    private void ParticleAmountFeedback()
    {
        //Get Boid Amount
        string particleAmountFieldText = ParticleAmountField.text;
        if (particleAmountFieldText != "")
        {
            int particleAmount = Int32.Parse(particleAmountFieldText);

            if (particleAmount > WarningLimit)
            {
                ParticleAmountFieldTextComponent.color = WarningColour;
            }
            else if (particleAmount > AdvisoryLimit)
            {
                ParticleAmountFieldTextComponent.color = AdvisoryColour;
            }
            else
            {
                ParticleAmountFieldTextComponent.color = StandardColour;
            }
        }
    }

    public void RespawnParticles()
    {
        string particleAmountFieldText = ParticleAmountField.text;
        if (particleAmountFieldText != "")
        {
            int particleAmount = Int32.Parse(particleAmountFieldText);
            
            //Limit To 67 Million (Max Thread Group Count * 1024)
            if (particleAmount > MaxParticles)
            {
                particleAmount = MaxParticles;
                ParticleAmountField.text = $"{MaxParticles}";
            }
            
            FlowFieldController.RespawnParticles(particleAmount);
        }
    }
}
