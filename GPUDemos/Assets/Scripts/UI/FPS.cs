using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour
{
        //VARIABLES
        public float UpdateRate = 4.0f; //How many times fps is updated per second
        private Text _fpsCounter;
        
        //FPS
        private int _fps;
        private int _frameCount;
        private float _deltaTime;
    
        private void Start()
        {
            _fpsCounter = GetComponent<Text>();
        }
    
        private void Update()
        {
            CalculateFPS();
        }
    
        private void CalculateFPS()
        {
            //Increment frame count and delta time
            _frameCount++;
            _deltaTime += Time.unscaledDeltaTime;
    
            //If 
            if (_deltaTime > 1.0 / UpdateRate)
            {
                _fps = Mathf.RoundToInt(_frameCount / _deltaTime);
                _frameCount = 0;
                _deltaTime -= 1.0f / UpdateRate;
            }
    
            _fpsCounter.text = $"{_fps}";
        }
}
