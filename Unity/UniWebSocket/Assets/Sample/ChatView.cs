using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UniWebSocket.Sample
{
    public class ChatView : MonoBehaviour, IChatView
    {
        [SerializeField] private InputField _uriInputField;
        [SerializeField] private InputField _nameInputField;
        [SerializeField] private InputField _chatTextInputField;

        [SerializeField] private Button _connectBotton;
        [SerializeField] private Button _closeBotton;
        [SerializeField] private Button _sendBotton;

        [SerializeField] private RectTransform _scrollViewContent;

        // 生成する要素
        [SerializeField] private RectTransform originalElement;
        [SerializeField] private Text elementOriginalText;
        
        public InputField UriInputField => _uriInputField;
        public InputField NameInputField => _nameInputField;
        public InputField ChatTextInputField => _chatTextInputField;
        public Button ConnectButton => _connectBotton;
        public Button CloseButton => _closeBotton;
        public Button SendButton => _sendBotton;
        IObservable<Unit> IChatView.OnDestroy => _onDestroySubject.AsObservable();

        private Subject<Unit> _onDestroySubject;

        private void Awake()
        {
            _onDestroySubject = new Subject<Unit>();
        }

        public void DrawNewMessage(string name, string message)
        {
        
            var contentString = $"<color=red>{name}</color>\n{message}";
            elementOriginalText.text = contentString;
            
            _chatTextInputField.text = string.Empty;
                
            RectTransform element = Instantiate(originalElement, _scrollViewContent, false);
            element.SetAsFirstSibling ();
            element.gameObject.SetActive (true);
        }


        public void OnDestroy()
        {
            _onDestroySubject.OnNext(Unit.Default);
            _onDestroySubject?.Dispose();
        }
    }
}