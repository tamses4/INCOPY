using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace WinFormsApp9

{

    public partial class Form1 : Form
    {
        private const int Port = 12345;
        private bool isRunning;
        private NotifyIcon notifyIcon;

        // Pour le hook du clavier
        private LowLevelKeyboardListener _keyboardListener;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private System.Windows.Forms.Timer clipboardTimer;
        private string lastClipboardContent;
        private TcpListener tcpListener;

        public Form1()
        {

            InitializeComponent();
            InitializeNotifyIcon();
            //StartPowerShellScript("FileReceiver.ps1");
            //AddToStartup(); // Ajouter l'application au démarrage
            //StartKeyboardListener();


        }

        private async void Form1_Load(object sender, EventArgs e)
        {

        }



        private async Task ScanNetworkAsync()
        {
            listView1.Items.Clear();
            string localIP = GetLocalIPAddress();
            if (string.IsNullOrEmpty(localIP))
            {
                MessageBox.Show("Impossible de détecter l'adresse IP locale. Vérifiez votre connexion réseau.",
                                "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string baseIP = localIP.Substring(0, localIP.LastIndexOf('.') + 1);
            List<Task> tasks = new List<Task>();
            for (int i = 1; i < 255; i++)
            {
                string ip = baseIP + i;
                tasks.Add(CheckHostAsync(ip));
            }

            await Task.WhenAll(tasks);

            if (listView1.Items.Count == 0)
            {
                listView1.Items.Add("Aucune machine détectée sur le réseau.");
            }
        }

        private async Task CheckHostAsync(string ip)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(ip, 200);
                if (reply.Status == IPStatus.Success)
                {
                    Invoke(new Action(() => listView1.Items.Add(ip)));
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs (erreurs de réseau, timeouts, etc.)
                Console.WriteLine($"Erreur lors du ping de {ip}: {ex.Message}");
            }
        }

        private string? GetLocalIPAddress()
        {
            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                        !networkInterface.Description.ToLower().Contains("virtual") &&
                        !networkInterface.Description.ToLower().Contains("vpn") &&
                        networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork &&
                                IsPrivateIP(unicastAddress.Address))
                            {
                                return unicastAddress.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la détection de l'adresse IP locale : {ex.Message}",
                                "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        private bool IsPrivateIP(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            CheckClipboardContent();
        }

        private void CheckClipboardContent()
        {
            try
            {
                // Vérifie si le presse-papiers contient du texte
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    // Compare le texte actuel avec le dernier contenu du presse-papiers
                    if (text != lastClipboardContent)
                    {
                        lastClipboardContent = text; // Met à jour le contenu du presse-papiers
                        listView2.Items.Add($"Texte détecté : {text}"); // Ajoute le texte à la liste
                        SendClipboardContent(text, false); // Envoie le texte à d'autres machines
                    }
                }
                // Vérifie si le presse-papiers contient une liste de fichiers
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files.Count > 0) // S'il y a des fichiers dans le presse-papiers
                    {

                        string filePath = files[0]; // Prend le premier fichier
                        string extension = Path.GetExtension(filePath).ToLower(); // Récupère l'extension du fichier
                                                                                  // Si le fichier est un fichier texte
                        if (extension == ".txt")
                        {
                            string fileContent = File.ReadAllText(filePath); // Lit le contenu du fichier texte
                            listView2.Items.Add($"Fichier texte détecté : {filePath}"); // Informe l'utilisateur
                            SendClipboardContent(fileContent, true); // Envoie le contenu du fichier texte
                        }
                        // Si le fichier est une image JPEG
                        else if (extension == ".jpg")
                        {
                            byte[] fileContent = File.ReadAllBytes(filePath); // Lit le fichier image en tant que tableau d'octets
                            listView2.Items.Add($"Fichier image détecté : {filePath}"); // Informe l'utilisateur
                            SendClipboardContent(fileContent, true, extension); // Envoie l'image
                        }
                        else
                        {
                            listView2.Items.Add($"Fichier détecté : {filePath}"); // Informe l'utilisateur pour d'autres types de fichiers
                            SendFilesToIP(filePath); // Envoie le fichier à une adresse IP spécifique
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                listView2.Items.Add($"Erreur : {ex.Message}"); // Gestion des exceptions
            }
        }

        private void SendClipboardContent(string content, bool isFile)
        {
            string localIP = GetLocalIPAddress();
            foreach (string ip in listView1.Items)
            {
                if (ip != localIP)
                {
                    SendTextToIP(ip, content, isFile);
                }
            }
        }

        private void SendClipboardContent(byte[] content, bool isFile, string extension)
        {
            string localIP = GetLocalIPAddress();
            foreach (string ip in listView1.Items)
            {
                if (ip != localIP)
                {
                    SendFileToIP(ip, content, extension);
                }
            }
        }

        private void SendTextToIP(string ip, string content, bool isFile)
        {
            try
            {
                using (TcpClient client = new TcpClient(ip, Port))
                {
                    NetworkStream stream = client.GetStream();
                    string message = isFile ? "FILE:" + content : content;
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi à {ip}: {ex.Message}");
            }
        }

        private void SendFileToIP(string ip, byte[] content, string extension)
        {
            try
            {
                using (TcpClient client = new TcpClient(ip, Port))
                {
                    NetworkStream stream = client.GetStream();
                    byte[] header = Encoding.UTF8.GetBytes("FILE:" + extension + ":");
                    stream.Write(header, 0, header.Length);
                    stream.Write(content, 0, content.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi à {ip}: {ex.Message}");
            }
        }

        private void SendFilesToIP(string filePath)
        {
            try
            {
                string localIP = GetLocalIPAddress();
                foreach (string ip in listView1.Items)
                {
                    if (ip != localIP)
                    {
                        using (TcpClient client = new TcpClient(ip, Port))
                        {
                            NetworkStream stream = client.GetStream();
                            byte[] fileData = File.ReadAllBytes(filePath);
                            byte[] header = Encoding.UTF8.GetBytes("FILE:" + Path.GetExtension(filePath) + ":");
                            stream.Write(header, 0, header.Length);
                            stream.Write(fileData, 0, fileData.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoi à {filePath}: {ex.Message}");
            }
        }

        private void StartTcpListener()
        {
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            Task.Run(() => ListenForClients());
        }

        private async Task ListenForClients()
        {
            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                using (MemoryStream ms = new MemoryStream())
                {
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                    }
                    byte[] receivedData = ms.ToArray();
                    string receivedMessage = Encoding.UTF8.GetString(receivedData);

                    if (receivedMessage.StartsWith("FILE:"))
                    {
                        string[] parts = receivedMessage.Split(new[] { ':' }, 3);
                        string extension = parts[1];
                        byte[] fileContent = receivedData.Skip(parts[0].Length + parts[1].Length + 2).ToArray();

                        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "reception");
                        Directory.CreateDirectory(downloadsPath);
                        string newFilePath = Path.Combine(downloadsPath, "received_file" + extension);
                        File.WriteAllBytes(newFilePath, fileContent);

                        Invoke(new Action(() =>
                        {
                            listView2.Items.Add($"Fichier {extension} reçu et créé : {newFilePath}");
                            Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection { newFilePath });
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            listView2.Items.Add($"Texte reçu : {receivedMessage}");
                            Clipboard.SetText(receivedMessage); // Mettre le texte dans le presse-papiers
                        }));
                    }
                }
            }
        }


        public void StartPowerShellScript(string scriptPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true // Ne pas afficher la fenêtre PowerShell
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du démarrage du script PowerShell : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("calculatrice-_1_.ico"), // Assurez-vous d'avoir un fichier icon.ico dans votre projet
                Visible = true,
                Text = "Serveur de partage de fichiers"
            };

            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // Créer le menu contextuel
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Quitter", null, (s, e) => CloseApplication());

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void StartKeyboardListener()
        {
            _keyboardListener = new LowLevelKeyboardListener();
            _keyboardListener.OnKeyPressed += KeyboardListener_OnKeyPressed;
            _keyboardListener.Start();
        }

        private void KeyboardListener_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            // Vérifie si Ctrl+C est pressé
            if (e.Key == Keys.C && e.IsCtrlPressed)
            {
                // Vérifie si un fichier a été copié
                if (Clipboard.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string filePath = files[0];

                    if (File.Exists(filePath))
                    {
                        var result = MessageBox.Show($"Le fichier '{Path.GetFileName(filePath)}' a été copié. Souhaitez-vous l'envoyer aux membres du réseau ?",
                                                      "Envoyer le fichier",
                                                      MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            SendFileToNetworkMembers(filePath);
                        }
                    }
                }
            }
        }

        public void SendFileToNetworkMembers(string filePath)
        {
            MessageBox.Show($"Le fichier '{Path.GetFileName(filePath)}' sera envoyé aux membres du réseau.", "Envoi en cours");

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);

                // Envoyer le fichier à chaque client connecté
                foreach (TcpClient client in connectedClients)
                {
                    SendFileToClient(client, fileName, fileBytes);
                }

                // Informer les clients du succès de l'envoi
                foreach (TcpClient client in connectedClients)
                {
                    SendSuccessMessage(client, fileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'envoi du fichier : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendFileToClient(TcpClient client, string fileName, byte[] fileBytes)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    // Envoyer le nom du fichier
                    SendMessageToClient(stream, $"FICHIER:{fileName}");

                    // Envoyer le contenu du fichier
                    stream.Write(fileBytes, 0, fileBytes.Length);

                    // Commande pour copier le fichier dans le presse-papiers (exécuter un script)
                    SendMessageToClient(stream, $"COPIER:{fileName}");
                }
                catch (Exception ex)
                {
                    SendErrorMessageToClient(stream, $"Erreur lors de l'envoi du fichier : {ex.Message}");
                }
            }
        }

        private void SendSuccessMessage(TcpClient client, string fileName)
        {
            using (NetworkStream stream = client.GetStream())
            {
                SendMessageToClient(stream, $"SUCCES:Le fichier '{fileName}' a été envoyé avec succès.");
            }
        }

        private void SendErrorMessageToClient(NetworkStream stream, string errorMessage)
        {
            SendMessageToClient(stream, $"ERREUR:{errorMessage}");
        }

        private void SendMessageToClient(NetworkStream stream, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }

        // Méthodes pour ajouter ou retirer des clients de la liste
        public void AddClient(TcpClient client)
        {
            connectedClients.Add(client);
        }

        public void RemoveClient(TcpClient client)
        {
            connectedClients.Remove(client);
        }




        private void CloseApplication()
        {
            notifyIcon.Visible = false; // Cacher l'icône d'abord
            Application.Exit(); // Quitter l'application
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false; // Désactiver le bouton
            await Task.Run(() => StartServer());
            btnStart.Enabled = true; // Réactiver le bouton
        }

        private void StartServer()
        {
            isRunning = true;
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Invoke(new Action(() => txtOutput.AppendText("Serveur démarré, en attente de connexions...\n")));

            while (isRunning)
            {
                if (listener.Pending())
                {
                    TcpClient client = listener.AcceptTcpClient();
                    HandleClient1(client);
                }
            }

            listener.Stop();
        }

        private void HandleClient1(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Supposons que la demande est au format "ENVOYER:filePath"
                string[] parts = request.Split(new[] { ':' }, 2);

                if (parts.Length == 2 && parts[0].Equals("ENVOYER", StringComparison.OrdinalIgnoreCase))
                {
                    string filePath = parts[1]; // Chemin du fichier à envoyer
                    SendFileToClient(client, filePath);
                }
                else
                {
                    // Si la demande est mal formée, envoyer un message d'erreur
                    string errorResponse = "Demande invalide. Utilisez le format : ENVOYER:filePath";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                    stream.Write(errorBytes, 0, errorBytes.Length);
                }
            }

            client.Close();
        }

        private void SendFileToClient(TcpClient client, string filePath)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    // Vérifier si le fichier existe
                    if (!File.Exists(filePath))
                    {
                        string errorResponse = "Erreur : Le fichier n'existe pas.";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                        stream.Write(errorBytes, 0, errorBytes.Length);
                        return;
                    }

                    // Lire le fichier et l'envoyer au client
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    stream.Write(fileBytes, 0, fileBytes.Length);

                    // Envoyer un message de confirmation
                    string successResponse = "Fichier envoyé avec succès.";
                    byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                    stream.Write(successBytes, 0, successBytes.Length);
                }
                catch (Exception ex)
                {
                    // En cas d'erreur, envoyer un message d'erreur au client
                    string errorResponse = $"Erreur lors de l'envoi du fichier : {ex.Message}";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                    stream.Write(errorBytes, 0, errorBytes.Length);
                }
            }
        }

        private void AddToStartup()
        {
            using (var registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                // Assurez-vous que le nom du registre correspond à votre application
                registryKey.SetValue("WindowsForms9", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true; // Annuler la fermeture de la fenêtre
            this.Hide(); // Cacher la fenêtre au lieu de la fermer
            notifyIcon.Visible = true; // S'assurer que l'icône est visible
        }



        private void label1_Click(object sender, EventArgs e)
        {

        }

        private async void Form1_Load_1(object sender, EventArgs e)
        {
            await ScanNetworkAsync();

            // Initialisation et démarrage du Timer pour vérifier le presse-papiers toutes les secondes
            clipboardTimer = new Timer();
            clipboardTimer.Interval = 6000; // 1 seconde
            clipboardTimer.Tick += ClipboardTimer_Tick;
            clipboardTimer.Start();

            // Démarrage du serveur TCP pour recevoir les données
            StartTcpListener();

        }

        private async void button1_Click(object sender, EventArgs e)
        {

            button1.Enabled = false;
            listView1.Items.Clear(); // Nettoyer les anciennes données
            await ScanNetworkAsync();
            button1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CloseApplication();
        }
    }

    // Classe pour le hook du clavier
    public class LowLevelKeyboardListener
    {
        public event EventHandler<KeyPressedEventArgs> OnKeyPressed;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public void Start()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);
                OnKeyPressed?.Invoke(this, new KeyPressedEventArgs(key));
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class KeyPressedEventArgs : EventArgs
    {
        public Keys Key { get; }
        public bool IsCtrlPressed => (Control.ModifierKeys & Keys.Control) == Keys.Control;

        public KeyPressedEventArgs(Keys key)
        {
            Key = key;
        }
    }
}