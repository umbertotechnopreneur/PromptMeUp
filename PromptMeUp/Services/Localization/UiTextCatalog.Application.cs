// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language application UI catalog.</summary>
    private static void AddApplicationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("About.License", new(
            English: "License",
            Italian: "Licenza",
            French: "Licence",
            German: "Lizenz",
            Spanish: "Licencia",
            Vietnamese: "Giấy phép"));
        entries.Add("About.Note", new(
            English: "Local-first console assistance. Commands always require your explicit approval.",
            Italian: "Assistenza console locale-first. I comandi richiedono sempre la tua approvazione esplicita.",
            French: "Assistance console locale d'abord. Les commandes nécessitent toujours votre accord explicite.",
            German: "Lokale Hilfe für die Konsole. Befehle erfordern immer Ihre ausdrückliche Zustimmung.",
            Spanish: "Asistencia de consola local-first. Los comandos siempre requieren tu aprobación explícita.",
            Vietnamese: "Trợ giúp console ưu tiên cục bộ. Lệnh luôn cần sự chấp thuận rõ ràng của bạn."));
        entries.Add("About.Repository", new(
            English: "GitHub repository",
            Italian: "Repository GitHub",
            French: "Dépôt GitHub",
            German: "GitHub-Repository",
            Spanish: "Repositorio GitHub",
            Vietnamese: "Kho GitHub"));
        entries.Add("About.Title", new(
            English: "About PromptMeUp",
            Italian: "Informazioni su PromptMeUp",
            French: "À propos de PromptMeUp",
            German: "Über PromptMeUp",
            Spanish: "Acerca de PromptMeUp",
            Vietnamese: "Giới thiệu PromptMeUp"));
        entries.Add("About.Website", new(
            English: "Creator site",
            Italian: "Sito di Umberto Giacobbi",
            French: "Site du créateur",
            German: "Website des Erstellers",
            Spanish: "Sitio del creador",
            Vietnamese: "Trang web tác giả"));
        entries.Add("Common.Back", new(
            English: "Back",
            Italian: "Indietro",
            French: "Retour",
            German: "Zurück",
            Spanish: "Atrás",
            Vietnamese: "Quay lại"));
        entries.Add("Common.Cancel", new(
            English: "Cancel",
            Italian: "Annulla",
            French: "Annuler",
            German: "Abbrechen",
            Spanish: "Cancelar",
            Vietnamese: "Hủy"));
        entries.Add("Common.Cancelled", new(
            English: "Current operation cancelled.",
            Italian: "Operazione corrente annullata.",
            French: "Opération en cours annulée.",
            German: "Aktueller Vorgang abgebrochen.",
            Spanish: "Operación actual cancelada.",
            Vietnamese: "Đã hủy thao tác hiện tại."));
        entries.Add("Common.Error", new(
            English: "ERROR",
            Italian: "ERRORE",
            French: "ERREUR",
            German: "FEHLER",
            Spanish: "ERROR",
            Vietnamese: "LỖI"));
        entries.Add("Common.No", new(
            English: "No",
            Italian: "No",
            French: "Non",
            German: "Nein",
            Spanish: "No",
            Vietnamese: "Không"));
        entries.Add("Common.Unknown", new(
            English: "unknown",
            Italian: "sconosciuto",
            French: "inconnu",
            German: "unbekannt",
            Spanish: "desconocido",
            Vietnamese: "không rõ"));
        entries.Add("Common.Yes", new(
            English: "Yes",
            Italian: "Sì",
            French: "Oui",
            German: "Ja",
            Spanish: "Sí",
            Vietnamese: "Có"));
        entries.Add("Footer.Command", new(
            English: "Command",
            Italian: "Comando",
            French: "Commande",
            German: "Befehl",
            Spanish: "Comando",
            Vietnamese: "Lệnh"));
        entries.Add("Footer.Data", new(
            English: "Data",
            Italian: "Dati",
            French: "Données",
            German: "Daten",
            Spanish: "Datos",
            Vietnamese: "Dữ liệu"));
        entries.Add("Footer.Support", new(
            English: "Enjoy the tool? Give it a star on GitHub or send us your suggestions.",
            Italian: "Apprezzi il tool? Lascia una stella su GitHub o inviaci i tuoi suggerimenti.",
            French: "Vous appréciez cet outil ? Ajoutez une étoile sur GitHub ou envoyez-nous vos suggestions.",
            German: "Gefällt Ihnen das Tool? Geben Sie ihm einen Stern auf GitHub oder senden Sie uns Ihre Vorschläge.",
            Spanish: "¿Te gusta la herramienta? Dale una estrella en GitHub o envíanos tus sugerencias.",
            Vietnamese: "Bạn thích công cụ này? Hãy tặng một sao trên GitHub hoặc gửi góp ý cho chúng tôi."));
        entries.Add("Footer.Thanks", new(
            English: "Thank you for using hm (help me)!",
            Italian: "Grazie per aver utilizzato hm (help me)!",
            French: "Merci d’avoir utilisé hm (help me) !",
            German: "Vielen Dank, dass Sie hm (help me) verwenden!",
            Spanish: "¡Gracias por utilizar hm (help me)!",
            Vietnamese: "Cảm ơn bạn đã sử dụng hm (help me)!"));
        entries.Add("Footer.Version", new(
            English: "Version",
            Italian: "Versione",
            French: "Version",
            German: "Version",
            Spanish: "Versión",
            Vietnamese: "Phiên bản"));
        entries.Add("Help.Chat", new(
            English: "Open an interactive multi-turn chat.",
            Italian: "Apre una chat interattiva multi-turno.",
            French: "Ouvre un chat interactif à plusieurs tours.",
            German: "Öffnet einen interaktiven Mehrfachrunden-Chat.",
            Spanish: "Abre un chat interactivo de varios turnos.",
            Vietnamese: "Mở chat tương tác nhiều lượt."));
        entries.Add("Help.Costs", new(
            English: "Refresh and show prices, usage, and monthly costs.",
            Italian: "Aggiorna e mostra prezzi, utilizzo e costi mensili.",
            French: "Actualise et affiche les prix, l'utilisation et les coûts mensuels.",
            German: "Aktualisiert und zeigt Preise, Nutzung und Monatskosten.",
            Spanish: "Actualiza y muestra precios, uso y costes mensuales.",
            Vietnamese: "Cập nhật và hiển thị giá, mức sử dụng và chi phí tháng."));
        entries.Add("Help.DryRun", new(
            English: "Preview font installation without changing the PC.",
            Italian: "Simula l'installazione font senza modificare il PC.",
            French: "Simule l'installation de la police sans modifier le système.",
            German: "Simuliert die Schriftinstallation ohne Systemänderung.",
            Spanish: "Simula la instalación de la fuente sin cambiar el sistema.",
            Vietnamese: "Mô phỏng cài font mà không thay đổi hệ thống."));
        entries.Add("Help.ExamplePrompt", new(
            English: "how do I undo the last local git commit?",
            Italian: "come annullo l'ultimo commit Git locale?",
            French: "comment annuler le dernier commit Git local ?",
            German: "wie mache ich den letzten lokalen Git-Commit rückgängig?",
            Spanish: "¿cómo deshago el último commit Git local?",
            Vietnamese: "làm cách nào để hoàn tác commit Git cục bộ gần nhất?"));
        entries.Add("Help.Examples", new(
            English: "Start here",
            Italian: "Per iniziare",
            French: "Pour commencer",
            German: "Erste Schritte",
            Spanish: "Para empezar",
            Vietnamese: "Bắt đầu tại đây"));
        entries.Add("Help.Font", new(
            English: "Install JetBrainsMono Nerd Font (opt-in).",
            Italian: "Installa JetBrainsMono Nerd Font (opt-in).",
            French: "Installe JetBrainsMono Nerd Font avec consentement.",
            German: "Installiert JetBrainsMono Nerd Font nach Zustimmung.",
            Spanish: "Instala JetBrainsMono Nerd Font con autorización.",
            Vietnamese: "Cài JetBrainsMono Nerd Font khi được cho phép."));
        entries.Add("Help.Group.Ai", new(
            English: "AI assistance",
            Italian: "Assistenza AI",
            French: "Assistance IA",
            German: "KI-Unterstützung",
            Spanish: "Asistencia de IA",
            Vietnamese: "Trợ giúp AI"));
        entries.Add("Help.Group.Insight", new(
            English: "Product and insight",
            Italian: "Prodotto e informazioni",
            French: "Produit et informations",
            German: "Produkt und Informationen",
            Spanish: "Producto e información",
            Vietnamese: "Sản phẩm và thông tin"));
        entries.Add("Help.Group.Safety", new(
            English: "Safety and terminal compatibility",
            Italian: "Sicurezza e compatibilità terminale",
            French: "Sécurité et compatibilité du terminal",
            German: "Sicherheit und Terminal-Kompatibilität",
            Spanish: "Seguridad y compatibilidad de terminal",
            Vietnamese: "An toàn và tương thích terminal"));
        entries.Add("Help.Group.Setup", new(
            English: "Setup and portability",
            Italian: "Configurazione e portabilità",
            French: "Configuration et portabilité",
            German: "Einrichtung und Portabilität",
            Spanish: "Configuración y portabilidad",
            Vietnamese: "Thiết lập và tính di động"));
        entries.Add("Help.Language", new(
            English: "Override the UI language: it, en, fr, de, es, vi.",
            Italian: "Sovrascrive la lingua UI: it, en, fr, de, es, vi.",
            French: "Remplace la langue de l'interface : it, en, fr, de, es, vi.",
            German: "Überschreibt die UI-Sprache: it, en, fr, de, es, vi.",
            Spanish: "Cambia el idioma de la interfaz: it, en, fr, de, es, vi.",
            Vietnamese: "Đổi ngôn ngữ giao diện: it, en, fr, de, es, vi."));
        entries.Add("Help.LanguageSyntax", new(
            English: "--language, -l <code>",
            Italian: "--language, -l <codice>",
            French: "--language, -l <code>",
            German: "--language, -l <Code>",
            Spanish: "--language, -l <código>",
            Vietnamese: "--language, -l <mã>"));
        entries.Add("Help.Path", new(
            English: "Preview, install, remove, or inspect the portable hm PATH entry.",
            Italian: "Mostra, aggiunge o rimuove dal PATH la cartella portabile di hm.",
            French: "Prévisualise, ajoute, retire ou vérifie le dossier portable hm dans PATH.",
            German: "Zeigt, ergänzt, entfernt oder prüft den portablen hm-Pfad.",
            Spanish: "Previsualiza, añade, elimina o comprueba la carpeta portátil hm en PATH.",
            Vietnamese: "Xem trước, thêm, xóa hoặc kiểm tra thư mục hm trong PATH."));
        entries.Add("Help.Query", new(
            English: "Send one prompt to the configured model.",
            Italian: "Invia un singolo prompt al modello configurato.",
            French: "Envoie un prompt unique au modèle configuré.",
            German: "Sendet einen einzelnen Prompt an das konfigurierte Modell.",
            Spanish: "Envía un prompt único al modelo configurado.",
            Vietnamese: "Gửi một prompt đến mô hình đã cấu hình."));
        entries.Add("Help.QuerySyntax", new(
            English: "--query, -q <text>",
            Italian: "--query, -q <testo>",
            French: "--query, -q <texte>",
            German: "--query, -q <Text>",
            Spanish: "--query, -q <texto>",
            Vietnamese: "--query, -q <văn-bản>"));
        entries.Add("Help.Rendering", new(
            English: "Disable animation or emoji for constrained terminals.",
            Italian: "Disabilita animazioni o emoji per terminali limitati.",
            French: "Désactive les animations ou les emoji pour les terminaux limités.",
            German: "Deaktiviert Animationen oder Emoji für eingeschränkte Terminals.",
            Spanish: "Desactiva animaciones o emoji para terminales limitados.",
            Vietnamese: "Tắt hoạt ảnh hoặc emoji cho terminal hạn chế."));
        entries.Add("Help.Setup", new(
            English: "Open Settings with General selected.",
            Italian: "Apre Impostazioni con Generali selezionato.",
            French: "Ouvre les réglages sur la section Général.",
            German: "Öffnet die Einstellungen mit dem Abschnitt Allgemein.",
            Spanish: "Abre Configuración con General seleccionado.",
            Vietnamese: "Mở Cài đặt và chọn mục Chung."));
        entries.Add("Help.Status", new(
            English: "Show configuration and local service status.",
            Italian: "Mostra configurazione e servizi locali.",
            French: "Affiche la configuration et les services locaux.",
            German: "Zeigt Konfiguration und lokale Dienste.",
            Spanish: "Muestra la configuración y los servicios locales.",
            Vietnamese: "Hiển thị cấu hình và dịch vụ cục bộ."));
        entries.Add("Help.Test", new(
            English: "Run the YAML connection prompt with teletype output.",
            Italian: "Esegue il prompt YAML di test con effetto teletype.",
            French: "Exécute le prompt YAML de connexion avec effet téléscripteur.",
            German: "Führt den YAML-Verbindungsprompt mit Fernschreiber-Effekt aus.",
            Spanish: "Ejecuta el prompt YAML de conexión con efecto teletipo.",
            Vietnamese: "Chạy prompt YAML kiểm tra kết nối với hiệu ứng máy đánh chữ."));
        entries.Add("Help.Lenna", new(
            English: "Show the bundled Lenna calibration portrait centered in the terminal. Works offline.",
            Italian: "Mostra al centro del terminale il ritratto di calibrazione Lenna incluso. Funziona offline.",
            French: "Affiche au centre du terminal le portrait de calibration Lenna fourni. Fonctionne hors ligne.",
            German: "Zeigt das mitgelieferte Lenna-Kalibrierporträt mittig im Terminal. Funktioniert offline.",
            Spanish: "Muestra centrado en el terminal el retrato de calibración Lenna incluido. Funciona sin conexión.",
            Vietnamese: "Hiển thị ảnh chân dung hiệu chuẩn Lenna đi kèm ở giữa terminal. Hoạt động ngoại tuyến."));
        entries.Add("Help.ThirdParty", new(
            English: "Show direct dependencies and third-party licenses.",
            Italian: "Mostra dipendenze dirette e licenze di terze parti.",
            French: "Affiche les dépendances directes et les licences tierces.",
            German: "Zeigt direkte Abhängigkeiten und Drittanbieter-Lizenzen.",
            Spanish: "Muestra dependencias directas y licencias de terceros.",
            Vietnamese: "Hiển thị phụ thuộc trực tiếp và giấy phép bên thứ ba."));
        entries.Add("Help.Title", new(
            English: "Command line",
            Italian: "Riga di comando",
            French: "Ligne de commande",
            German: "Befehlszeile",
            Spanish: "Línea de comandos",
            Vietnamese: "Dòng lệnh"));
        entries.Add("Help.Usage", new(
            English: "Usage: hm [command] [options]",
            Italian: "Uso: hm [comando] [opzioni]",
            French: "Utilisation : hm [commande] [options]",
            German: "Verwendung: hm [Befehl] [Optionen]",
            Spanish: "Uso: hm [comando] [opciones]",
            Vietnamese: "Cách dùng: hm [lệnh] [tùy chọn]"));
        entries.Add("Help.Version", new(
            English: "Show application and runtime versions.",
            Italian: "Mostra le versioni dell'applicazione e del runtime.",
            French: "Affiche les versions de l'application et du runtime.",
            German: "Zeigt Anwendungs- und Laufzeitversionen.",
            Spanish: "Muestra las versiones de la aplicación y del runtime.",
            Vietnamese: "Hiển thị phiên bản ứng dụng và runtime."));
        entries.Add("Help.Where", new(
            English: "Show where hm is running from and offer folder actions.",
            Italian: "Mostra da dove viene eseguito hm e propone azioni sulla cartella.",
            French: "Affiche l'emplacement de hm et propose des actions sur son dossier.",
            German: "Zeigt den Ausführungsort von hm und bietet Ordneraktionen an.",
            Spanish: "Muestra desde dónde se ejecuta hm y ofrece acciones para su carpeta.",
            Vietnamese: "Hiển thị nơi hm đang chạy và các thao tác với thư mục."));
        entries.Add("Help.Yes", new(
            English: "Preauthorize only an explicit PATH or font operation; never a chat command.",
            Italian: "Preautorizza solo un'operazione PATH o font esplicita; mai un comando della chat.",
            French: "Préautorise uniquement une opération PATH ou police explicite, jamais une commande de chat.",
            German: "Autorisiert nur eine explizite PATH- oder Schriftoperation vorab, niemals einen Chat-Befehl.",
            Spanish: "Preautoriza solo una operación PATH o fuente explícita; nunca un comando del chat.",
            Vietnamese: "Chỉ cấp quyền trước cho thao tác PATH hoặc font rõ ràng; không bao giờ cho lệnh chat."));
        entries.Add("Main.Chat", new(
            English: "Start AI chat",
            Italian: "Avvia chat AI",
            French: "Démarrer le chat IA",
            German: "KI-Chat starten",
            Spanish: "Iniciar chat de IA",
            Vietnamese: "Bắt đầu chat AI"));
        entries.Add("Main.Choose", new(
            English: "Choose an action",
            Italian: "Scegli un'azione",
            French: "Choisissez une action",
            German: "Aktion auswählen",
            Spanish: "Elige una acción",
            Vietnamese: "Chọn tác vụ"));
        entries.Add("Main.Costs", new(
            English: "Usage and costs",
            Italian: "Utilizzo e costi",
            French: "Coûts OpenAI",
            German: "Nutzung und Kosten",
            Spanish: "Costes de OpenAI",
            Vietnamese: "Sử dụng và chi phí"));
        entries.Add("Main.Exit", new(
            English: "Exit",
            Italian: "Esci",
            French: "Quitter",
            German: "Beenden",
            Spanish: "Salir",
            Vietnamese: "Thoát"));
        entries.Add("Main.Font", new(
            English: "Install Nerd Font",
            Italian: "Installa Nerd Font",
            French: "Installer Nerd Font",
            German: "Nerd Font installieren",
            Spanish: "Instalar Nerd Font",
            Vietnamese: "Cài Nerd Font"));
        entries.Add("Main.Query", new(
            English: "Single prompt",
            Italian: "Prompt singolo",
            French: "Prompt unique",
            German: "Einzelner Prompt",
            Spanish: "Prompt único",
            Vietnamese: "Prompt đơn"));
        entries.Add("Main.Setup", new(
            English: "Settings",
            Italian: "Impostazioni",
            French: "Réglages",
            German: "Einstellungen",
            Spanish: "Configuración",
            Vietnamese: "Cài đặt"));
        entries.Add("Main.Status", new(
            English: "Application status",
            Italian: "Stato applicazione",
            French: "État de l'application",
            German: "Anwendungsstatus",
            Spanish: "Estado de la aplicación",
            Vietnamese: "Trạng thái ứng dụng"));
        entries.Add("Main.Test", new(
            English: "Test AI connection",
            Italian: "Test connessione AI",
            French: "Tester la connexion IA",
            German: "KI-Verbindung testen",
            Spanish: "Probar conexión de IA",
            Vietnamese: "Kiểm tra kết nối AI"));
        entries.Add("Main.Title", new(
            English: "Command center",
            Italian: "Centro comandi",
            French: "Centre de commandes",
            German: "Kommandozentrale",
            Spanish: "Centro de comandos",
            Vietnamese: "Trung tâm lệnh"));
        entries.Add("Main.Where", new(
            English: "Locate hm",
            Italian: "Trova hm",
            French: "Localiser hm",
            German: "hm finden",
            Spanish: "Localizar hm",
            Vietnamese: "Tìm hm"));
        entries.Add("Navigation.Shortcuts", new(
            English: "Esc back · Ctrl+C exit",
            Italian: "Esc indietro · Ctrl+C esci",
            French: "Esc retour · Ctrl+C quitter",
            German: "Esc zurück · Ctrl+C beenden",
            Spanish: "Esc volver · Ctrl+C salir",
            Vietnamese: "Esc quay lại · Ctrl+C thoát"));
        entries.Add("Tagline", new(
            English: "Prompt engineering, conversation, and cost insight — locally organized.",
            Italian: "Prompt engineering, conversazioni e controllo costi — organizzati in locale.",
            French: "Ingénierie de prompts, conversations et suivi des coûts — organisés localement.",
            German: "Prompt-Engineering, Gespräche und Kostenkontrolle — lokal organisiert.",
            Spanish: "Ingeniería de prompts, conversación y control de costes — organizado localmente.",
            Vietnamese: "Kỹ thuật prompt, hội thoại và theo dõi chi phí — được tổ chức cục bộ."));
        entries.Add("Shell.OpeningKicker", new(
            English: "YOUR LOCAL AI COMMAND WORKSPACE",
            Italian: "IL TUO SPAZIO DI LAVORO AI LOCALE",
            French: "VOTRE ESPACE DE TRAVAIL IA LOCAL",
            German: "DEIN LOKALER KI-ARBEITSBEREICH",
            Spanish: "TU ESPACIO DE TRABAJO LOCAL CON IA",
            Vietnamese: "KHÔNG GIAN LÀM VIỆC AI CỤC BỘ CỦA BẠN"));
        entries.Add("ThirdParty.FullNotices", new(
            English: "Full notices: THIRD_PARTY_NOTICES.md",
            Italian: "Note complete: THIRD_PARTY_NOTICES.md",
            French: "Mentions complètes : THIRD_PARTY_NOTICES.md",
            German: "Vollständige Hinweise: THIRD_PARTY_NOTICES.md",
            Spanish: "Avisos completos: THIRD_PARTY_NOTICES.md",
            Vietnamese: "Thông báo đầy đủ: THIRD_PARTY_NOTICES.md"));
        entries.Add("ThirdParty.License", new(
            English: "License",
            Italian: "Licenza",
            French: "Licence",
            German: "Lizenz",
            Spanish: "Licencia",
            Vietnamese: "Giấy phép"));
        entries.Add("ThirdParty.Package", new(
            English: "Package",
            Italian: "Pacchetto",
            French: "Paquet",
            German: "Paket",
            Spanish: "Paquete",
            Vietnamese: "Gói"));
        entries.Add("ThirdParty.Subtitle", new(
            English: "Direct runtime dependencies used by PromptMeUp.",
            Italian: "Dipendenze runtime dirette usate da PromptMeUp.",
            French: "Dépendances d'exécution directes utilisées par PromptMeUp.",
            German: "Direkte Laufzeitabhängigkeiten von PromptMeUp.",
            Spanish: "Dependencias directas de ejecución usadas por PromptMeUp.",
            Vietnamese: "Các phụ thuộc runtime trực tiếp được PromptMeUp sử dụng."));
        entries.Add("ThirdParty.Title", new(
            English: "Third-party software",
            Italian: "Software di terze parti",
            French: "Logiciels tiers",
            German: "Drittanbieter-Software",
            Spanish: "Software de terceros",
            Vietnamese: "Phần mềm bên thứ ba"));
        entries.Add("ThirdParty.Version", new(
            English: "Version",
            Italian: "Versione",
            French: "Version",
            German: "Version",
            Spanish: "Versión",
            Vietnamese: "Phiên bản"));
    }
}
