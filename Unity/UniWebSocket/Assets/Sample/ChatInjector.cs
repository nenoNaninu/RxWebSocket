using RxWebSocket.Logging;
using UnityEngine;

namespace RxWebSocket.Sample
{
    public class ChatInjector : MonoBehaviour
    {
        private void Start()
        {
            var view = gameObject.GetComponent<IChatView>();
            var presenter = new ChatPresenter(view);
            
            var chatClient = new ChatClient(new UnityConsoleLogger());

            var useCase = new ChatUseCase(presenter, chatClient);
            
        }
    }
}