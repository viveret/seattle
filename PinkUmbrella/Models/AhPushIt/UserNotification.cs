using PinkUmbrella.Models.Public;

namespace PinkUmbrella.Models.AhPushIt
{
    public class UserNotification
    {
        public UserNotification(NotificationRecipient r, Notification n)
        {
            User = r;
            Notif = n;
        }

        public NotificationRecipient User { get; set; }

        public Notification Notif { get; set; }
        
        public PublicProfileModel FromUser { get; set; }
    }
}