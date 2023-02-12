using Godot;

namespace Game
{
    public class IPLabelController : Label
    {
        public override void _Ready()
        {
            string ipAddress;
            switch (OS.GetName())
            {
                case "Windows":
                    ipAddress = IP.GetLocalAddresses()[3].ToString();
                    break;
                case "Android":
                    ipAddress = IP.GetLocalAddresses()[0].ToString();
                    break;
                default:
                    ipAddress = IP.GetLocalAddresses()[3].ToString();
                    break;
            }

            foreach (string ip in IP.GetLocalAddresses())
                if (ip.BeginsWith("192.168.") && !ip.EndsWith(".1"))
                    ipAddress = ip;

            if (ipAddress.BeginsWith("192.168"))
            {
                Text = $"IP: {ipAddress}";
            }
            else
            {
                Text = "IP:\n";
                foreach (string ip in IP.GetLocalAddresses())
                    if (!ip.Contains(":") && (ip.BeginsWith("172") || ip.BeginsWith("192")))
                        Text += ip + "\n";
            }

        }
    }
}