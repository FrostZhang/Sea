using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Basic message for sending instructions to motions
    /// </summary>
    public class CameraMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_CAMERA_MOTOR_UNKNOWN = 200;
        public static int MSG_CAMERA_MOTOR_ACTIVATE = 201;
        public static int MSG_CAMERA_MOTOR_DEACTIVATE = 202;
        public static int MSG_CAMERA_MOTOR_TEST = 203;

        public static string[] Names = new string[]
        {
            "Unknown",
            "Motor Activate",
            "Motor Deactivate",
            "Motor Test"
        };

        /// <summary>
        /// Motion the message is referencing
        /// </summary>
        public CameraMotor Motor = null;

        /// <summary>
        /// Determines if the motion should continue
        /// </summary>
        public bool Continue = false;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Motor = null;
            Continue = false;

            base.Clear();
        }

        /// <summary>
        /// Release this instance.
        /// </summary>
        public override void Release()
        {
            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            IsSent = true;
            IsHandled = true;

            // Make it available to others.
            if (this is CameraMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<CameraMessage> sPool = new ObjectPool<CameraMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static CameraMessage Allocate()
        {
            // Grab the next available object
            CameraMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new CameraMessage(); }

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static CameraMessage Allocate(CameraMessage rSource)
        {
            // Grab the next available object
            CameraMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new CameraMessage(); }

            lInstance.ID = rSource.ID;
            lInstance.Motor = rSource.Motor;
            lInstance.Continue = rSource.Continue;

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(CameraMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            sPool.Release(rInstance);
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public new static void Release(IMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            if (rInstance is CameraMessage)
            {
                sPool.Release((CameraMessage)rInstance);
            }
        }
    }
}
