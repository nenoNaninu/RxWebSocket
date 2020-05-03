using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace RxWebSocket.Sample
{
    public class ChatView : MonoBehaviour, IChatView
    {
        [SerializeField] private InputField _uriInputField;
        [SerializeField] private InputField _nameInputField;
        [SerializeField] private InputField _chatTextInputField;

        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _sendButton;

        [SerializeField] private RectTransform _scrollViewContent;

        // 生成する要素
        [SerializeField] private RectTransform originalElement;
        [SerializeField] private Text elementOriginalText;
        
        public InputField UriInputField => _uriInputField;
        public InputField NameInputField => _nameInputField;
        public InputField ChatTextInputField => _chatTextInputField;
        public Button ConnectButton => _connectButton;
        public Button CloseButton => _closeButton;
        public Button SendButton => _sendButton;
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