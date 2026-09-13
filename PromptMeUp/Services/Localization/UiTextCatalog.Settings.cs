// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the shared settings workspace labels and section guidance in all six languages.</summary>
    private static void AddSettingsEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Settings.Title", new(
            English: "Settings",
            Italian: "Impostazioni",
            French: "Réglages",
            German: "Einstellungen",
            Spanish: "Configuración",
            Vietnamese: "Cài đặt"));
        entries.Add("Settings.General", new(
            English: "General",
            Italian: "Generali",
            French: "Général",
            German: "Allgemein",
            Spanish: "General",
            Vietnamese: "Chung"));
        entries.Add("Settings.Ai", new(
            English: "AI",
            Italian: "AI",
            French: "IA",
            German: "KI",
            Spanish: "IA",
            Vietnamese: "AI"));
        entries.Add("Settings.Credentials", new(
            English: "Credentials",
            Italian: "Credenziali",
            French: "Identifiants",
            German: "Zugangsdaten",
            Spanish: "Credenciales",
            Vietnamese: "Thông tin xác thực"));
        entries.Add("Settings.Context", new(
            English: "Conversation",
            Italian: "Conversazione",
            French: "Conversation",
            German: "Gespräch",
            Spanish: "Conversación",
            Vietnamese: "Trò chuyện"));
        entries.Add("Settings.Commands", new(
            English: "Commands",
            Italian: "Comandi",
            French: "Commandes",
            German: "Befehle",
            Spanish: "Comandos",
            Vietnamese: "Lệnh"));
        entries.Add("Settings.Personalization", new(
            English: "Personalization",
            Italian: "Personalizzazione",
            French: "Personnalisation",
            German: "Personalisierung",
            Spanish: "Personalización",
            Vietnamese: "Cá nhân hóa"));
        entries.Add("Settings.Theme", new(
            English: "Theme",
            Italian: "Tema",
            French: "Thème",
            German: "Design",
            Spanish: "Tema",
            Vietnamese: "Giao diện"));
        entries.Add("Settings.GeneralHelp", new(
            English: "Choose the language for your interface and conversations.",
            Italian: "Scegli la lingua per l'interfaccia e le conversazioni.",
            French: "Choisissez la langue de l'interface et des conversations.",
            German: "Wählen Sie die Sprache für die Oberfläche und Gespräche.",
            Spanish: "Elige el idioma de la interfaz y las conversaciones.",
            Vietnamese: "Chọn ngôn ngữ cho giao diện và các cuộc trò chuyện."));
        entries.Add("Settings.AiHelp", new(
            English: "Enable AI and choose its model, responses, advisory command review, and caching.",
            Italian: "Abilita l'AI e scegli modello, risposte, revisione consultiva dei comandi e cache.",
            French: "Activez l'IA et choisissez le modèle, les réponses, l'analyse consultative des commandes et le cache.",
            German: "Aktivieren Sie KI und wählen Sie Modell, Antworten, beratende Befehlsprüfung und Zwischenspeicherung.",
            Spanish: "Activa la IA y elige modelo, respuestas, revisión orientativa de comandos y caché.",
            Vietnamese: "Bật AI và chọn mô hình, cách trả lời, đánh giá lệnh tham khảo và bộ nhớ đệm."));
        entries.Add("Settings.CredentialsHelp", new(
            English: "Manage keys and the optional connection check. Leave replacements blank to keep existing keys.",
            Italian: "Gestisci chiavi e verifica facoltativa della connessione. Lascia vuote le sostituzioni per mantenere le chiavi esistenti.",
            French: "Gérez les clés et le test de connexion facultatif. Laissez les remplacements vides pour conserver les clés existantes.",
            German: "Verwalten Sie Schlüssel und den optionalen Verbindungstest. Leere Ersatzfelder behalten vorhandene Schlüssel bei.",
            Spanish: "Gestiona las claves y la prueba de conexión opcional. Deja vacíos los reemplazos para conservar las claves existentes.",
            Vietnamese: "Quản lý khóa và kiểm tra kết nối tùy chọn. Để trống các trường thay thế để giữ khóa hiện có."));
        entries.Add("Settings.ContextHelp", new(
            English: "Set how much conversation each request can retain.",
            Italian: "Imposta quanta conversazione può conservare ogni richiesta.",
            French: "Définissez la quantité de conversation conservée pour chaque requête.",
            German: "Legen Sie fest, wie viel Gespräch jede Anfrage behalten darf.",
            Spanish: "Define cuánta conversación puede conservar cada solicitud.",
            Vietnamese: "Đặt lượng nội dung trò chuyện mỗi yêu cầu có thể giữ lại."));
        entries.Add("Settings.CommandsHelp", new(
            English: "Set the command timeout and how much output to retain.",
            Italian: "Imposta il timeout dei comandi e quanto output conservare.",
            French: "Définissez le délai d'exécution des commandes et la quantité de sortie à conserver.",
            German: "Legen Sie das Befehlszeitlimit und die Menge der gespeicherten Ausgabe fest.",
            Spanish: "Define el tiempo de espera de los comandos y cuánta salida conservar.",
            Vietnamese: "Đặt thời gian chờ của lệnh và lượng đầu ra cần giữ lại."));
        entries.Add("Settings.PersonalizationHelp", new(
            English: "Add personal instructions and choose whether to share location context.",
            Italian: "Aggiungi istruzioni personali e scegli se condividere il contesto della posizione.",
            French: "Ajoutez des instructions personnelles et choisissez de partager ou non le contexte de localisation.",
            German: "Ergänzen Sie persönliche Anweisungen und wählen Sie, ob Standortkontext geteilt wird.",
            Spanish: "Añade instrucciones personales y elige si compartes el contexto de ubicación.",
            Vietnamese: "Thêm hướng dẫn cá nhân và chọn có chia sẻ ngữ cảnh vị trí hay không."));
        entries.Add("Settings.ThemeHelp", new(
            English: "Preview a color palette; it becomes permanent only when you save settings.",
            Italian: "Visualizza una palette colori; diventa permanente solo quando salvi le impostazioni.",
            French: "Prévisualisez une palette ; elle devient permanente après l'enregistrement des réglages.",
            German: "Sehen Sie eine Farbpalette an; sie wird erst beim Speichern der Einstellungen dauerhaft übernommen.",
            Spanish: "Previsualiza una paleta de colores; solo se aplica de forma permanente al guardar la configuración.",
            Vietnamese: "Xem trước bảng màu; bảng màu chỉ được áp dụng lâu dài khi bạn lưu cài đặt."));
        entries.Add("Settings.ThemePreview", new(
            English: "Palette preview",
            Italian: "Anteprima palette",
            French: "Aperçu de la palette",
            German: "Palettenvorschau",
            Spanish: "Vista previa de la paleta",
            Vietnamese: "Xem trước bảng màu"));
    }
}
