// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language system integration UI catalog.</summary>
    private static void AddSystemIntegrationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Font.Confirm", new(
            English: "Install JetBrainsMono Nerd Font for the current user?",
            Italian: "Installare JetBrainsMono Nerd Font per l'utente corrente?",
            French: "Installer JetBrainsMono Nerd Font pour l'utilisateur actuel ?",
            German: "JetBrainsMono Nerd Font für den aktuellen Benutzer installieren?",
            Spanish: "¿Instalar JetBrainsMono Nerd Font para el usuario actual?",
            Vietnamese: "Cài JetBrainsMono Nerd Font cho người dùng hiện tại?"));
        entries.Add("Font.DryRun", new(
            English: "DRY RUN",
            Italian: "SIMULAZIONE",
            French: "SIMULATION",
            German: "SIMULATION",
            Spanish: "SIMULACIÓN",
            Vietnamese: "MÔ PHỎNG"));
        entries.Add("Font.LookupTimeout", new(
            English: "Font prerequisite lookup timed out.",
            Italian: "La verifica dei prerequisiti del font ha superato il tempo limite.",
            French: "La vérification des prérequis de la police a expiré.",
            German: "Die Prüfung der Schriftvoraussetzungen hat das Zeitlimit überschritten.",
            Spanish: "La comprobación de requisitos de la fuente agotó el tiempo.",
            Vietnamese: "Kiểm tra điều kiện cài phông chữ đã hết thời gian."));
        entries.Add("Font.Preview", new(
            English: "Dry run complete; no system changes were made.",
            Italian: "Simulazione completata; nessuna modifica al sistema.",
            French: "Simulation terminée ; aucune modification système.",
            German: "Simulation beendet; keine Systemänderung.",
            Spanish: "Simulación completada; no se cambió el sistema.",
            Vietnamese: "Đã mô phỏng; không thay đổi hệ thống."));
        entries.Add("Font.Progress", new(
            English: "Installing terminal font...",
            Italian: "Installazione font terminale...",
            French: "Installation de la police terminal...",
            German: "Terminal-Schrift wird installiert...",
            Spanish: "Instalando fuente del terminal...",
            Vietnamese: "Đang cài font terminal..."));
        entries.Add("Font.Ready", new(
            English: "Font ready: {0}",
            Italian: "Font pronto: {0}",
            French: "Police prête : {0}",
            German: "Schrift bereit: {0}",
            Spanish: "Fuente lista: {0}",
            Vietnamese: "Font đã sẵn sàng: {0}"));
        entries.Add("Font.TerminalHint", new(
            English: "Select the installed Nerd Font in your terminal profile, then reopen the terminal.",
            Italian: "Seleziona il Nerd Font installato nel profilo del terminale, poi riapri il terminale.",
            French: "Sélectionnez la Nerd Font dans le profil du terminal puis rouvrez-le.",
            German: "Die installierte Nerd Font im Terminalprofil auswählen und das Terminal neu öffnen.",
            Spanish: "Selecciona la Nerd Font instalada en el perfil del terminal y vuelve a abrirlo.",
            Vietnamese: "Chọn Nerd Font trong hồ sơ terminal rồi mở lại terminal."));
        entries.Add("Font.Timeout", new(
            English: "Font installation timed out after two minutes.",
            Italian: "L'installazione del font ha superato il limite di due minuti.",
            French: "L'installation de la police a dépassé deux minutes.",
            German: "Die Schriftinstallation hat das Zeitlimit von zwei Minuten überschritten.",
            Spanish: "La instalación de la fuente superó el límite de dos minutos.",
            Vietnamese: "Cài đặt phông chữ đã quá thời hạn hai phút."));
        entries.Add("Font.Title", new(
            English: "Nerd Font installer",
            Italian: "Installer Nerd Font",
            French: "Installation Nerd Font",
            German: "Nerd-Font-Installer",
            Spanish: "Instalador de Nerd Font",
            Vietnamese: "Trình cài Nerd Font"));
        entries.Add("Font.Unsupported", new(
            English: "Automatic installation is unavailable on this platform; use its font manager.",
            Italian: "L'installazione automatica non è disponibile su questa piattaforma; usa il relativo gestore dei font.",
            French: "L'installation automatique n'est pas disponible sur cette plateforme ; utilisez son gestionnaire de polices.",
            German: "Die automatische Installation ist auf dieser Plattform nicht verfügbar; verwenden Sie deren Schriftverwaltung.",
            Spanish: "La instalación automática no está disponible en esta plataforma; usa su gestor de fuentes.",
            Vietnamese: "Không thể cài đặt tự động trên nền tảng này; hãy dùng trình quản lý phông chữ."));
        entries.Add("Path.Action", new(
            English: "Choose a PATH action",
            Italian: "Scegli un'azione PATH",
            French: "Choisissez une action PATH",
            German: "PATH-Aktion auswählen",
            Spanish: "Elige una acción de PATH",
            Vietnamese: "Chọn thao tác PATH"));
        entries.Add("Path.Confirm", new(
            English: "Apply this exact PATH change?",
            Italian: "Applicare esattamente questa modifica al PATH?",
            French: "Appliquer exactement cette modification du PATH ?",
            German: "Genau diese PATH-Änderung anwenden?",
            Spanish: "¿Aplicar exactamente este cambio de PATH?",
            Vietnamese: "Áp dụng chính xác thay đổi PATH này?"));
        entries.Add("Path.Directory", new(
            English: "hm directory",
            Italian: "Cartella di hm",
            French: "Dossier de hm",
            German: "hm-Ordner",
            Spanish: "Carpeta de hm",
            Vietnamese: "Thư mục hm"));
        entries.Add("Path.Install", new(
            English: "Add this hm folder to the user PATH",
            Italian: "Aggiungi questa cartella hm al PATH utente",
            French: "Ajouter ce dossier hm au PATH utilisateur",
            German: "Diesen hm-Ordner zum Benutzer-PATH hinzufügen",
            Spanish: "Añadir esta carpeta hm al PATH del usuario",
            Vietnamese: "Thêm thư mục hm này vào PATH người dùng"));
        entries.Add("Path.Missing", new(
            English: "This hm folder is not in the persistent user PATH.",
            Italian: "Questa cartella hm non è nel PATH utente persistente.",
            French: "Ce dossier hm n'est pas dans le PATH utilisateur persistant.",
            German: "Dieser hm-Ordner ist nicht im persistenten Benutzer-PATH.",
            Spanish: "Esta carpeta hm no está en el PATH persistente del usuario.",
            Vietnamese: "Thư mục hm này chưa có trong PATH người dùng lâu dài."));
        entries.Add("Path.Present", new(
            English: "This hm folder is already in the persistent user PATH.",
            Italian: "Questa cartella hm è già nel PATH utente persistente.",
            French: "Ce dossier hm est déjà dans le PATH utilisateur persistant.",
            German: "Dieser hm-Ordner ist bereits im persistenten Benutzer-PATH.",
            Spanish: "Esta carpeta hm ya está en el PATH persistente del usuario.",
            Vietnamese: "Thư mục hm này đã có trong PATH người dùng lâu dài."));
        entries.Add("Path.Preview.Install", new(
            English: "Add the hm directory to the persistent user PATH.",
            Italian: "Aggiungi la cartella di hm al PATH utente persistente.",
            French: "Ajouter le dossier de hm au PATH utilisateur persistant.",
            German: "Den hm-Ordner zum persistenten Benutzer-PATH hinzufügen.",
            Spanish: "Añade la carpeta de hm al PATH persistente del usuario.",
            Vietnamese: "Thêm thư mục hm vào PATH người dùng lâu dài."));
        entries.Add("Path.Preview.Remove", new(
            English: "Remove the managed hm directory from the persistent user PATH.",
            Italian: "Rimuovi la cartella di hm gestita dal PATH utente persistente.",
            French: "Retirer le dossier de hm géré du PATH utilisateur persistant.",
            German: "Den verwalteten hm-Ordner aus dem persistenten Benutzer-PATH entfernen.",
            Spanish: "Elimina la carpeta gestionada de hm del PATH persistente del usuario.",
            Vietnamese: "Xóa thư mục hm được quản lý khỏi PATH người dùng lâu dài."));
        entries.Add("Path.Preview.Status", new(
            English: "Inspect whether the hm directory is in the persistent user PATH.",
            Italian: "Controlla se la cartella di hm è presente nel PATH utente persistente.",
            French: "Vérifier si le dossier de hm se trouve dans le PATH utilisateur persistant.",
            German: "Prüfen, ob der hm-Ordner im persistenten Benutzer-PATH enthalten ist.",
            Spanish: "Comprueba si la carpeta de hm está en el PATH persistente del usuario.",
            Vietnamese: "Kiểm tra thư mục hm có trong PATH người dùng lâu dài hay không."));
        entries.Add("Path.Remove", new(
            English: "Remove the managed hm folder from the user PATH",
            Italian: "Rimuovi dal PATH utente la cartella hm gestita",
            French: "Retirer du PATH utilisateur le dossier hm géré",
            German: "Den verwalteten hm-Ordner aus dem Benutzer-PATH entfernen",
            Spanish: "Eliminar del PATH del usuario la carpeta hm gestionada",
            Vietnamese: "Xóa thư mục hm được quản lý khỏi PATH người dùng"));
        entries.Add("Path.Status", new(
            English: "Inspect PATH status",
            Italian: "Controlla lo stato PATH",
            French: "Vérifier l'état du PATH",
            German: "PATH-Status prüfen",
            Spanish: "Comprobar el estado de PATH",
            Vietnamese: "Kiểm tra trạng thái PATH"));
        entries.Add("Path.Target", new(
            English: "Persistent target",
            Italian: "Destinazione persistente",
            French: "Cible persistante",
            German: "Persistentes Ziel",
            Spanish: "Destino persistente",
            Vietnamese: "Đích lâu dài"));
        entries.Add("Path.Title", new(
            English: "Portable PATH",
            Italian: "PATH portabile",
            French: "PATH portable",
            German: "Portabler PATH",
            Spanish: "PATH portátil",
            Vietnamese: "PATH di động"));
        entries.Add("Path.WindowsUserTarget", new(
            English: "Windows user PATH",
            Italian: "PATH utente Windows",
            French: "PATH utilisateur Windows",
            German: "Windows-Benutzer-PATH",
            Spanish: "PATH de usuario de Windows",
            Vietnamese: "PATH người dùng Windows"));
        entries.Add("Where.Action", new(
            English: "What would you like to do?",
            Italian: "Cosa vuoi fare?",
            French: "Que souhaitez-vous faire ?",
            German: "Was möchten Sie tun?",
            Spanish: "¿Qué quieres hacer?",
            Vietnamese: "Bạn muốn làm gì?"));
        entries.Add("Where.ChangeDirectoryHint", new(
            English: "A child process cannot change this terminal. Run this command here:",
            Italian: "Un processo figlio non può cambiare questo terminale. Esegui qui il comando:",
            French: "Un processus enfant ne peut pas changer ce terminal. Exécutez ici :",
            German: "Ein Kindprozess kann dieses Terminal nicht ändern. Führen Sie hier aus:",
            Spanish: "Un proceso secundario no puede cambiar este terminal. Ejecuta aquí:",
            Vietnamese: "Tiến trình con không thể đổi terminal này. Hãy chạy lệnh sau tại đây:"));
        entries.Add("Where.Confirm", new(
            English: "Open the system file manager now?",
            Italian: "Aprire ora Esplora file?",
            French: "Ouvrir maintenant le gestionnaire de fichiers ?",
            German: "Den Dateimanager jetzt öffnen?",
            Spanish: "¿Abrir ahora el explorador de archivos?",
            Vietnamese: "Mở trình quản lý tệp ngay bây giờ?"));
        entries.Add("Where.Directory", new(
            English: "Directory",
            Italian: "Cartella",
            French: "Dossier",
            German: "Ordner",
            Spanish: "Carpeta",
            Vietnamese: "Thư mục"));
        entries.Add("Where.Executable", new(
            English: "Executable",
            Italian: "Eseguibile",
            French: "Exécutable",
            German: "Ausführbare Datei",
            Spanish: "Ejecutable",
            Vietnamese: "Tệp thực thi"));
        entries.Add("Where.None", new(
            English: "Do nothing",
            Italian: "Non fare nulla",
            French: "Ne rien faire",
            German: "Nichts tun",
            Spanish: "No hacer nada",
            Vietnamese: "Không làm gì"));
        entries.Add("Where.Open", new(
            English: "Open and select hm in the system file manager",
            Italian: "Apri e seleziona hm in Esplora file",
            French: "Ouvrir et sélectionner hm dans le gestionnaire de fichiers",
            German: "hm im Dateimanager öffnen und auswählen",
            Spanish: "Abrir y seleccionar hm en el explorador de archivos",
            Vietnamese: "Mở và chọn hm trong trình quản lý tệp"));
        entries.Add("Where.OpenPreview", new(
            English: "Exact launch preview",
            Italian: "Anteprima esatta apertura",
            French: "Aperçu exact de l'ouverture",
            German: "Exakte Startvorschau",
            Spanish: "Vista previa exacta de apertura",
            Vietnamese: "Xem trước lệnh mở chính xác"));
        entries.Add("Where.Opened", new(
            English: "The executable folder was opened.",
            Italian: "La cartella dell'eseguibile è stata aperta.",
            French: "Le dossier de l'exécutable a été ouvert.",
            German: "Der Ordner der ausführbaren Datei wurde geöffnet.",
            Spanish: "Se abrió la carpeta del ejecutable.",
            Vietnamese: "Đã mở thư mục chứa tệp thực thi."));
        entries.Add("Where.ShowCd", new(
            English: "Show a command to change this terminal's directory",
            Italian: "Mostra il comando per cambiare cartella in questo terminale",
            French: "Afficher la commande pour changer le dossier de ce terminal",
            German: "Befehl zum Wechseln des Ordners in diesem Terminal anzeigen",
            Spanish: "Mostrar el comando para cambiar la carpeta de este terminal",
            Vietnamese: "Hiển thị lệnh đổi thư mục cho terminal này"));
        entries.Add("Where.Title", new(
            English: "hm location",
            Italian: "Posizione di hm",
            French: "Emplacement de hm",
            German: "hm-Speicherort",
            Spanish: "Ubicación de hm",
            Vietnamese: "Vị trí hm"));
    }
}
