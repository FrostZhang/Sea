using System;
using UnityEngine.Events;

namespace com.ootii.Messages
{
    [Serializable]
    public class MessageEvent : UnityEvent<IMessage>
    {
    }
}
