// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language status UI catalog.</summary>
    private static void AddStatusEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Shell.ActiveContext", new(
            English: "Active context / model",
            Italian: "Contesto attivo / modello",
            French: "Contexte actif / modèle",
            German: "Aktiver Kontext / Modell",
            Spanish: "Contexto activo / modelo",
            Vietnamese: "Ngữ cảnh hiện tại / mô hình"));
        entries.Add("Shell.Application", new(
            English: "Application",
            Italian: "Applicazione",
            French: "Application",
            German: "Anwendung",
            Spanish: "Aplicación",
            Vietnamese: "Ứng dụng"));
        entries.Add("Shell.Cache", new(
            English: "Cache read/write",
            Italian: "Cache lettura/scrittura",
            French: "Cache lecture/écriture",
            German: "Cache Lesen/Schreiben",
            Spanish: "Caché lectura/escritura",
            Vietnamese: "Cache đọc/ghi"));
        entries.Add("Shell.Context", new(
            English: "Total context",
            Italian: "Contesto totale",
            French: "Contexte total",
            German: "Gesamtkontext",
            Spanish: "Contexto total",
            Vietnamese: "Tổng ngữ cảnh"));
        entries.Add("Shell.ContextBudget", new(
            English: "Operating budget",
            Italian: "Budget operativo",
            French: "Budget de travail",
            German: "Arbeitsbudget",
            Spanish: "Presupuesto operativo",
            Vietnamese: "Ngân sách làm việc"));
        entries.Add("Shell.CurrentDirectory", new(
            English: "Current directory",
            Italian: "Cartella corrente",
            French: "Dossier courant",
            German: "Aktuelles Verzeichnis",
            Spanish: "Carpeta actual",
            Vietnamese: "Thư mục hiện tại"));
        entries.Add("Shell.Input", new(
            English: "Input tokens",
            Italian: "Token in",
            French: "Jetons entrants",
            German: "Eingabe-Token",
            Spanish: "Tokens de entrada",
            Vietnamese: "Token đầu vào"));
        entries.Add("Shell.LastInput", new(
            English: "Last call input",
            Italian: "Input ultima chiamata",
            French: "Entrée du dernier appel",
            German: "Letzte Anfrage: Eingabe",
            Spanish: "Entrada de última llamada",
            Vietnamese: "Đầu vào lần gọi cuối"));
        entries.Add("Shell.LastOutput", new(
            English: "Last call output",
            Italian: "Output ultima chiamata",
            French: "Sortie du dernier appel",
            German: "Letzte Anfrage: Ausgabe",
            Spanish: "Salida de última llamada",
            Vietnamese: "Đầu ra lần gọi cuối"));
        entries.Add("Shell.MemoryUsage", new(
            English: "Loaded memories",
            Italian: "Memorie caricate",
            French: "Mémoires chargées",
            German: "Geladene Erinnerungen",
            Spanish: "Memorias cargadas",
            Vietnamese: "Ghi nhớ đã nạp"));
        entries.Add("Shell.MemoryUsageValue", new(
            English: "{0} notes · ~{1} / 800 tokens",
            Italian: "{0} note · ~{1} / 800 token",
            French: "{0} notes · ~{1} / 800 jetons",
            German: "{0} Notizen · ~{1} / 800 Tokens",
            Spanish: "{0} notas · ~{1} / 800 tokens",
            Vietnamese: "{0} ghi chú · ~{1} / 800 token"));
        entries.Add("Shell.Model", new(
            English: "Model",
            Italian: "Modello",
            French: "Modèle",
            German: "Modell",
            Spanish: "Modelo",
            Vietnamese: "Mô hình"));
        entries.Add("Shell.Output", new(
            English: "Output tokens",
            Italian: "Token out",
            French: "Jetons sortants",
            German: "Ausgabe-Token",
            Spanish: "Tokens de salida",
            Vietnamese: "Token đầu ra"));
        entries.Add("Shell.Platform", new(
            English: "Platform",
            Italian: "Piattaforma",
            French: "Plateforme",
            German: "Plattform",
            Spanish: "Plataforma",
            Vietnamese: "Nền tảng"));
        entries.Add("Shell.Runtime", new(
            English: "Runtime",
            Italian: "Runtime",
            French: "Runtime",
            German: "Runtime",
            Spanish: "Runtime",
            Vietnamese: "Runtime"));
        entries.Add("Shell.Session", new(
            English: "Session snapshot",
            Italian: "Specchietto sessione",
            French: "Aperçu de session",
            German: "Sitzungsübersicht",
            Spanish: "Resumen de sesión",
            Vietnamese: "Tóm tắt phiên"));
        entries.Add("Shell.SessionCost", new(
            English: "Session cost",
            Italian: "Costo sessione",
            French: "Coût de session",
            German: "Sitzungskosten",
            Spanish: "Coste de sesión",
            Vietnamese: "Chi phí phiên"));
        entries.Add("Shell.SessionInput", new(
            English: "Session input total",
            Italian: "Input totale sessione",
            French: "Total des entrées de session",
            German: "Sitzung: Eingaben gesamt",
            Spanish: "Entrada total de sesión",
            Vietnamese: "Tổng đầu vào phiên"));
        entries.Add("Shell.SessionOutput", new(
            English: "Session output total",
            Italian: "Output totale sessione",
            French: "Total des sorties de session",
            German: "Sitzung: Ausgaben gesamt",
            Spanish: "Salida total de sesión",
            Vietnamese: "Tổng đầu ra phiên"));
        entries.Add("Shell.Thinking", new(
            English: "Thinking",
            Italian: "Ragionamento",
            French: "Réflexion",
            German: "Denken",
            Spanish: "Razonamiento",
            Vietnamese: "Lập luận"));
        entries.Add("Shell.TurnCost", new(
            English: "This turn",
            Italian: "Questo turno",
            French: "Ce tour",
            German: "Diese Runde",
            Spanish: "Este turno",
            Vietnamese: "Lượt này"));
        entries.Add("Status.AdminKey", new(
            English: "Admin key",
            Italian: "Chiave admin",
            French: "Clé admin",
            German: "Admin-Schlüssel",
            Spanish: "Clave de administrador",
            Vietnamese: "Khóa quản trị"));
        entries.Add("Status.ApiKey", new(
            English: "API key",
            Italian: "Chiave API",
            French: "Clé API",
            German: "API-Schlüssel",
            Spanish: "Clave API",
            Vietnamese: "Khóa API"));
        entries.Add("Status.Completed", new(
            English: "completed",
            Italian: "completata",
            French: "terminée",
            German: "abgeschlossen",
            Spanish: "completada",
            Vietnamese: "đã hoàn tất"));
        entries.Add("Status.Configuration", new(
            English: "Configuration",
            Italian: "Configurazione",
            French: "Configuration",
            German: "Konfiguration",
            Spanish: "Configuración",
            Vietnamese: "Cấu hình"));
        entries.Add("Status.Database", new(
            English: "Database",
            Italian: "Database",
            French: "Base de données",
            German: "Datenbank",
            Spanish: "Base de datos",
            Vietnamese: "Cơ sở dữ liệu"));
        entries.Add("Status.Disabled", new(
            English: "disabled",
            Italian: "disabilitata",
            French: "désactivée",
            German: "deaktiviert",
            Spanish: "desactivada",
            Vietnamese: "đã tắt"));
        entries.Add("Status.Language", new(
            English: "Language",
            Italian: "Lingua",
            French: "Langue",
            German: "Sprache",
            Spanish: "Idioma",
            Vietnamese: "Ngôn ngữ"));
        entries.Add("Status.Local", new(
            English: "local",
            Italian: "locale",
            French: "locale",
            German: "lokal",
            Spanish: "local",
            Vietnamese: "cục bộ"));
        entries.Add("Status.LocalData", new(
            English: "Local data",
            Italian: "Dati locali",
            French: "Données locales",
            German: "Lokale Daten",
            Spanish: "Datos locales",
            Vietnamese: "Dữ liệu cục bộ"));
        entries.Add("Status.Logs", new(
            English: "Logs",
            Italian: "Log",
            French: "Journaux",
            German: "Protokolle",
            Spanish: "Registros",
            Vietnamese: "Nhật ký"));
        entries.Add("Status.Missing", new(
            English: "missing",
            Italian: "mancante",
            French: "manquante",
            German: "fehlt",
            Spanish: "ausente",
            Vietnamese: "thiếu"));
        entries.Add("Status.Model", new(
            English: "Model",
            Italian: "Modello",
            French: "Modèle",
            German: "Modell",
            Spanish: "Modelo",
            Vietnamese: "Mô hình"));
        entries.Add("Status.Pricing", new(
            English: "Pricing cache",
            Italian: "Cache prezzi",
            French: "Cache des prix",
            German: "Preis-Cache",
            Spanish: "Caché de precios",
            Vietnamese: "Bộ nhớ đệm giá"));
        entries.Add("Status.Prompts", new(
            English: "YAML prompts",
            Italian: "Prompt YAML",
            French: "Prompts YAML",
            German: "YAML-Prompts",
            Spanish: "Prompts YAML",
            Vietnamese: "Prompt YAML"));
        entries.Add("Status.Ready", new(
            English: "ready",
            Italian: "pronta",
            French: "prête",
            German: "bereit",
            Spanish: "lista",
            Vietnamese: "sẵn sàng"));
        entries.Add("Status.Required", new(
            English: "required",
            Italian: "richiesta",
            French: "requise",
            German: "erforderlich",
            Spanish: "requerida",
            Vietnamese: "bắt buộc"));
        entries.Add("Status.Setup", new(
            English: "Setup",
            Italian: "Configurazione",
            French: "Configuration",
            German: "Einrichtung",
            Spanish: "Configuración",
            Vietnamese: "Thiết lập"));
        entries.Add("Status.Thinking", new(
            English: "Thinking with the configured model...",
            Italian: "Elaborazione con il modello configurato...",
            French: "Réflexion avec le modèle configuré...",
            German: "Verarbeitung mit dem konfigurierten Modell...",
            Spanish: "Procesando con el modelo configurado...",
            Vietnamese: "Đang xử lý với mô hình đã cấu hình..."));
        entries.Add("Status.Title", new(
            English: "Application status",
            Italian: "Stato applicazione",
            French: "État de l'application",
            German: "Anwendungsstatus",
            Spanish: "Estado de la aplicación",
            Vietnamese: "Trạng thái ứng dụng"));
    }
}
