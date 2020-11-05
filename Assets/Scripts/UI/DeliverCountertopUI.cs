using Lean.Transition;
using UnityEngine;
using TMPro;

namespace Undercooked.UI
{
    public class DeliverCountertopUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        private static TextMeshProUGUI _text;

        [Header("notification colors")]
        [SerializeField] private Color positiveColorOutline;
        [SerializeField] private Color positiveColorBase;
        [SerializeField] private Color negativeColorOutline;
        [SerializeField] private Color negativeColorBase;
        
        private void Awake()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        private void OnEnable()
        {
            GameManager.OnTipCalculated += HandleTipCalculate;
            DeliverCountertop.OnPlateMissing += HandlePlateMissing;
        }

        private void OnDisable()
        {
            GameManager.OnTipCalculated -= HandleTipCalculate;
            DeliverCountertop.OnPlateMissing -= HandlePlateMissing;
        }

        private void HandlePlateMissing()
        {
            DisplayNotification($"NEEDS PLATE!", negativeColorBase, negativeColorOutline, 2f);
        }

        private void HandleTipCalculate(int tip)
        {
            DisplayNotification($"+{tip} TIP!", positiveColorBase, positiveColorOutline, 2f);
        }

        private void DisplayNotification(string textToDisplay, Color baseColor, Color outlineColor, float timeToDisplayInSeconds = 2f)
        {
            _text.gameObject.transform.localPosition = Vector3.zero;
            _canvasGroup.alpha = 1f;
            _text.text = textToDisplay;
            _text.color = baseColor;
            _text.outlineColor = outlineColor;
            _canvasGroup.alphaTransition(0f, timeToDisplayInSeconds, LeanEase.Smooth);
            _text.rectTransform
                .localPositionTransition_Y(100f, timeToDisplayInSeconds, LeanEase.Smooth);
        }
        
    }
    
}