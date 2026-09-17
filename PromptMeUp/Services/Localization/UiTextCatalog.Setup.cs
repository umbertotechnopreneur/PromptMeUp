// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language setup UI catalog.</summary>
    private static void AddSetupEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("AiSettings.ContextBudget", new(
            English: "Operating context budget (tokens)",
            Italian: "Budget operativo del contesto (token)",
            French: "Budget de contexte de travail (jetons)",
            German: "Kontext-Arbeitsbudget (Tokens)",
            Spanish: "Presupuesto operativo de contexto (tokens)",
            Vietnamese: "Ngân sách ngữ cảnh làm việc (token)"));
        entries.Add("AiSettings.ContextOverride", new(
            English: "PROMPTMEUP_CONTEXT_TOKENS overrides the saved context budget while it is set.",
            Italian: "PROMPTMEUP_CONTEXT_TOKENS sostituisce il budget del contesto salvato finché è impostata.",
            French: "PROMPTMEUP_CONTEXT_TOKENS remplace le budget de contexte enregistré tant que cette variable est définie.",
            German: "Solange PROMPTMEUP_CONTEXT_TOKENS gesetzt ist, ersetzt die Variable das gespeicherte Kontextbudget.",
            Spanish: "PROMPTMEUP_CONTEXT_TOKENS sustituye el presupuesto de contexto guardado mientras esté definida.",
            Vietnamese: "PROMPTMEUP_CONTEXT_TOKENS ghi đè ngân sách ngữ cảnh đã lưu khi biến này được đặt."));
        entries.Add("AiSettings.Help", new(
            English: "Open Settings with AI selected (--ai-setup or --ai-settings)",
            Italian: "Apre Impostazioni con AI selezionato (--ai-setup o --ai-settings)",
            French: "Ouvre les réglages sur la section IA (--ai-setup ou --ai-settings)",
            German: "Öffnet die Einstellungen mit dem Abschnitt KI (--ai-setup oder --ai-settings)",
            Spanish: "Abre Configuración con IA seleccionado (--ai-setup o --ai-settings)",
            Vietnamese: "Mở Cài đặt và chọn mục AI (--ai-setup hoặc --ai-settings)"));
        entries.Add("AiSettings.Saved", new(
            English: "AI settings saved.",
            Italian: "Impostazioni AI salvate.",
            French: "Paramètres IA enregistrés.",
            German: "KI-Einstellungen gespeichert.",
            Spanish: "Ajustes de IA guardados.",
            Vietnamese: "Đã lưu cài đặt AI."));
        entries.Add("AiSettings.Subtitle", new(
            English: "Choose how the assistant responds and how much context each request can use.",
            Italian: "Scegli come risponde l'assistente e quanto contesto può usare ogni richiesta.",
            French: "Choisissez la façon dont l'assistant répond et la quantité de contexte disponible par requête.",
            German: "Legen Sie fest, wie der Assistent antwortet und wie viel Kontext jede Anfrage verwenden darf.",
            Spanish: "Elige cómo responde el asistente y cuánto contexto puede usar cada solicitud.",
            Vietnamese: "Chọn cách trợ lý trả lời và lượng ngữ cảnh mỗi yêu cầu được sử dụng."));
        entries.Add("AiSettings.Title", new(
            English: "AI settings",
            Italian: "Impostazioni AI",
            French: "Paramètres IA",
            German: "KI-Einstellungen",
            Spanish: "Ajustes de IA",
            Vietnamese: "Cài đặt AI"));
        entries.Add("Model.gpt-5.4", new(
            English: "Affordable model for coding and professional work.",
            Italian: "Modello accessibile per programmazione e lavoro professionale.",
            French: "Modèle abordable pour le développement et le travail professionnel.",
            German: "Preiswertes Modell für Programmierung und professionelle Arbeit.",
            Spanish: "Modelo asequible para programación y trabajo profesional.",
            Vietnamese: "Mô hình có chi phí hợp lý cho lập trình và công việc chuyên môn."));
        entries.Add("Model.gpt-5.4-mini", new(
            English: "Efficient model for focused and high-volume tasks.",
            Italian: "Modello efficiente per attività mirate e ad alto volume.",
            French: "Modèle efficace pour les tâches ciblées et volumineuses.",
            German: "Effizientes Modell für fokussierte Aufgaben mit hohem Volumen.",
            Spanish: "Modelo eficiente para tareas específicas y de gran volumen.",
            Vietnamese: "Mô hình hiệu quả cho tác vụ tập trung và khối lượng lớn."));
        entries.Add("Model.gpt-5.4-nano", new(
            English: "Low-cost model for extraction, ranking, and simple tasks.",
            Italian: "Modello economico per estrazione, classificazione e attività semplici.",
            French: "Modèle économique pour l'extraction, le classement et les tâches simples.",
            German: "Kostengünstiges Modell für Extraktion, Rangfolge und einfache Aufgaben.",
            Spanish: "Modelo económico para extracción, clasificación y tareas sencillas.",
            Vietnamese: "Mô hình chi phí thấp cho trích xuất, xếp hạng và tác vụ đơn giản."));
        entries.Add("Model.gpt-5.5", new(
            English: "Previous frontier model for complex professional work.",
            Italian: "Precedente modello di frontiera per lavori professionali complessi.",
            French: "Modèle de pointe précédent pour les travaux professionnels complexes.",
            German: "Vorheriges Spitzenmodell für komplexe professionelle Arbeit.",
            Spanish: "Modelo de vanguardia anterior para trabajos profesionales complejos.",
            Vietnamese: "Mô hình hàng đầu thế hệ trước cho công việc chuyên môn phức tạp."));
        entries.Add("Model.gpt-5.6-luna", new(
            English: "Cost-sensitive model for fast, high-volume workloads.",
            Italian: "Modello conveniente per attività rapide e ad alto volume.",
            French: "Modèle économique pour les charges rapides et volumineuses.",
            German: "Kostenbewusstes Modell für schnelle Aufgaben mit hohem Volumen.",
            Spanish: "Modelo económico para cargas rápidas y de gran volumen.",
            Vietnamese: "Mô hình tiết kiệm cho khối lượng công việc lớn và cần tốc độ."));
        entries.Add("Model.gpt-5.6-sol", new(
            English: "Frontier model for demanding professional work and reasoning.",
            Italian: "Modello di frontiera per lavoro professionale e ragionamento impegnativi.",
            French: "Modèle de pointe pour les travaux professionnels et raisonnements exigeants.",
            German: "Spitzenmodell für anspruchsvolle professionelle Arbeit und Schlussfolgerungen.",
            Spanish: "Modelo de vanguardia para trabajo profesional y razonamiento exigentes.",
            Vietnamese: "Mô hình hàng đầu cho công việc chuyên môn và suy luận phức tạp."));
        entries.Add("Model.gpt-5.6-terra", new(
            English: "Balanced intelligence, latency, and cost for everyday work.",
            Italian: "Equilibrio tra intelligenza, latenza e costo per il lavoro quotidiano.",
            French: "Équilibre entre intelligence, latence et coût pour le travail quotidien.",
            German: "Ausgewogenes Verhältnis von Intelligenz, Latenz und Kosten im Alltag.",
            Spanish: "Equilibrio entre inteligencia, latencia y coste para el trabajo diario.",
            Vietnamese: "Cân bằng trí tuệ, độ trễ và chi phí cho công việc hằng ngày."));
        entries.Add("Reasoning.high", new(
            English: "High",
            Italian: "Alto",
            French: "Élevé",
            German: "Hoch",
            Spanish: "Alto",
            Vietnamese: "Cao"));
        entries.Add("Reasoning.low", new(
            English: "Low",
            Italian: "Basso",
            French: "Faible",
            German: "Niedrig",
            Spanish: "Bajo",
            Vietnamese: "Thấp"));
        entries.Add("Reasoning.max", new(
            English: "Maximum",
            Italian: "Massimo",
            French: "Maximum",
            German: "Maximum",
            Spanish: "Máximo",
            Vietnamese: "Tối đa"));
        entries.Add("Reasoning.medium", new(
            English: "Medium",
            Italian: "Medio",
            French: "Moyen",
            German: "Mittel",
            Spanish: "Medio",
            Vietnamese: "Trung bình"));
        entries.Add("Reasoning.none", new(
            English: "None",
            Italian: "Nessuno",
            French: "Aucun",
            German: "Keine",
            Spanish: "Ninguno",
            Vietnamese: "Không"));
        entries.Add("Reasoning.xhigh", new(
            English: "Very high",
            Italian: "Molto alto",
            French: "Très élevé",
            German: "Sehr hoch",
            Spanish: "Muy alto",
            Vietnamese: "Rất cao"));
        entries.Add("Setup.AdminKeyPrompt", new(
            English: "OpenAI admin key",
            Italian: "Chiave admin OpenAI",
            French: "Clé admin OpenAI",
            German: "OpenAI Admin-Schlüssel",
            Spanish: "Clave de administrador de OpenAI",
            Vietnamese: "Khóa quản trị OpenAI"));
        entries.Add("Setup.AdminKeyStatus", new(
            English: "OPENAI_ADMIN_KEY status",
            Italian: "Stato OPENAI_ADMIN_KEY",
            French: "État de OPENAI_ADMIN_KEY",
            German: "Status von OPENAI_ADMIN_KEY",
            Spanish: "Estado de OPENAI_ADMIN_KEY",
            Vietnamese: "Trạng thái OPENAI_ADMIN_KEY"));
        entries.Add("Setup.AiEnabled", new(
            English: "Enable the OpenAI provider?",
            Italian: "Abilitare il provider OpenAI?",
            French: "Activer le fournisseur OpenAI ?",
            German: "OpenAI-Anbieter aktivieren?",
            Spanish: "¿Activar el proveedor OpenAI?",
            Vietnamese: "Bật nhà cung cấp OpenAI?"));
        entries.Add("Setup.Balanced", new(
            English: "Balanced",
            Italian: "Bilanciato",
            French: "Équilibré",
            German: "Ausgewogen",
            Spanish: "Equilibrado",
            Vietnamese: "Cân bằng"));
        entries.Add("Setup.Cancelled", new(
            English: "Setup cancelled; no configuration was saved.",
            Italian: "Configurazione annullata; nessuna modifica salvata.",
            French: "Configuration annulée ; aucune modification enregistrée.",
            German: "Einrichtung abgebrochen; keine Änderungen gespeichert.",
            Spanish: "Configuración cancelada; no se guardaron cambios.",
            Vietnamese: "Đã hủy thiết lập; không lưu thay đổi."));
        entries.Add("Setup.CommandReview", new(
            English: "Use optional AI review before command authorization?",
            Italian: "Usare la revisione AI opzionale prima di autorizzare i comandi?",
            French: "Utiliser l'analyse IA facultative avant d'autoriser les commandes ?",
            German: "Optionale KI-Prüfung vor der Befehlsfreigabe verwenden?",
            Spanish: "¿Usar la revisión de IA opcional antes de autorizar comandos?",
            Vietnamese: "Dùng đánh giá AI tùy chọn trước khi cấp quyền chạy lệnh?"));
        entries.Add("Setup.CommandTimeout", new(
            English: "Command timeout in seconds",
            Italian: "Timeout comando in secondi",
            French: "Délai d'expiration des commandes en secondes",
            German: "Befehlszeitlimit in Sekunden",
            Spanish: "Tiempo de espera del comando en segundos",
            Vietnamese: "Thời gian chờ lệnh tính bằng giây"));
        entries.Add("Setup.Compact", new(
            English: "Compact · lowest usage",
            Italian: "Compatto · consumo minimo",
            French: "Compact · utilisation minimale",
            German: "Kompakt · geringste Nutzung",
            Spanish: "Compacto · uso mínimo",
            Vietnamese: "Gọn · ít token nhất"));
        entries.Add("Setup.Confirm", new(
            English: "Save these settings?",
            Italian: "Salvare queste impostazioni?",
            French: "Enregistrer ces paramètres ?",
            German: "Diese Einstellungen speichern?",
            Spanish: "¿Guardar esta configuración?",
            Vietnamese: "Lưu các cài đặt này?"));
        entries.Add("Setup.Custom", new(
            English: "AI preamble added to every chat and query (optional)",
            Italian: "Preambolo AI aggiunto a ogni chat e richiesta (opzionale)",
            French: "Préambule IA ajouté à chaque chat et requête (facultatif)",
            German: "KI-Präambel für jeden Chat und jede Anfrage (optional)",
            Spanish: "Preámbulo de IA añadido a cada chat y consulta (opcional)",
            Vietnamese: "Lời mở đầu AI được thêm vào mọi cuộc chat và truy vấn (tùy chọn)"));
        entries.Add("Setup.Detail", new(
            English: "Analysis detail",
            Italian: "Dettaglio analisi",
            French: "Détail de l'analyse",
            German: "Analysedetail",
            Spanish: "Detalle del análisis",
            Vietnamese: "Mức chi tiết"));
        entries.Add("Setup.Detailed", new(
            English: "Detailed · larger budget",
            Italian: "Dettagliato · budget maggiore",
            French: "Détaillé · budget supérieur",
            German: "Detailliert · größeres Budget",
            Spanish: "Detallado · presupuesto mayor",
            Vietnamese: "Chi tiết · ngân sách lớn hơn"));
        entries.Add("Setup.EditAdvanced", new(
            English: "Edit advanced limits and endpoint?",
            Italian: "Modificare limiti avanzati ed endpoint?",
            French: "Modifier les limites avancées et le point de terminaison ?",
            German: "Erweiterte Limits und Endpunkt bearbeiten?",
            Spanish: "¿Editar límites avanzados y endpoint?",
            Vietnamese: "Chỉnh sửa giới hạn nâng cao và endpoint?"));
        entries.Add("Setup.Endpoint", new(
            English: "OpenAI Responses endpoint",
            Italian: "Endpoint OpenAI Responses",
            French: "Point de terminaison OpenAI Responses",
            German: "OpenAI-Responses-Endpunkt",
            Spanish: "Endpoint de OpenAI Responses",
            Vietnamese: "Endpoint OpenAI Responses"));
        entries.Add("Setup.EndpointError", new(
            English: "Use the official https://api.openai.com/v1/responses endpoint.",
            Italian: "Usa l'endpoint ufficiale https://api.openai.com/v1/responses.",
            French: "Utilisez le point de terminaison officiel https://api.openai.com/v1/responses.",
            German: "Verwenden Sie den offiziellen Endpunkt https://api.openai.com/v1/responses.",
            Spanish: "Usa el endpoint oficial https://api.openai.com/v1/responses.",
            Vietnamese: "Hãy dùng endpoint chính thức https://api.openai.com/v1/responses."));
        entries.Add("Setup.Header", new(
            English: "SYSTEM CONFIGURATION",
            Italian: "CONFIGURAZIONE SISTEMA",
            French: "CONFIGURATION SYSTÈME",
            German: "SYSTEMKONFIGURATION",
            Spanish: "CONFIGURACIÓN DEL SISTEMA",
            Vietnamese: "CẤU HÌNH HỆ THỐNG"));
        entries.Add("Setup.KeyError", new(
            English: "The key must start with sk- and contain no whitespace.",
            Italian: "La chiave deve iniziare con sk- e non contenere spazi.",
            French: "La clé doit commencer par sk- et ne contenir aucun espace.",
            German: "Der Schlüssel muss mit sk- beginnen und darf keine Leerzeichen enthalten.",
            Spanish: "La clave debe comenzar por sk- y no contener espacios.",
            Vietnamese: "Khóa phải bắt đầu bằng sk- và không chứa khoảng trắng."));
        entries.Add("Setup.KeyPrompt", new(
            English: "OpenAI API key",
            Italian: "Chiave API OpenAI",
            French: "Clé API OpenAI",
            German: "OpenAI API-Schlüssel",
            Spanish: "Clave API de OpenAI",
            Vietnamese: "Khóa API OpenAI"));
        entries.Add("Setup.KeyRestartRequired", new(
            English: "The updated key is already available in this hm process. Before the next launch of hm, fully close and reopen the terminal application (including the IDE for an integrated terminal).",
            Italian: "La chiave aggiornata è già disponibile nel processo hm in esecuzione. Prima del prossimo avvio di hm, chiudi completamente e riapri l'applicazione del terminale (anche l'IDE se usi un terminale integrato).",
            French: "La clé mise à jour est déjà disponible dans ce processus hm. Avant le prochain lancement de hm, fermez complètement puis rouvrez l’application du terminal (y compris l’IDE pour un terminal intégré).",
            German: "Der aktualisierte Schlüssel ist in diesem hm-Prozess bereits verfügbar. Schließen Sie vor dem nächsten Start von hm die Terminalanwendung vollständig und öffnen Sie sie erneut (bei einem integrierten Terminal auch die IDE).",
            Spanish: "La clave actualizada ya está disponible en este proceso de hm. Antes del próximo inicio de hm, cierra por completo y vuelve a abrir la aplicación del terminal (también el IDE si usas un terminal integrado).",
            Vietnamese: "Khóa đã cập nhật có sẵn ngay trong tiến trình hm này. Trước lần khởi chạy hm tiếp theo, hãy đóng hoàn toàn rồi mở lại ứng dụng terminal (bao gồm cả IDE nếu dùng terminal tích hợp)."));
        entries.Add("Setup.KeyStatus", new(
            English: "OPENAI_API_KEY status",
            Italian: "Stato OPENAI_API_KEY",
            French: "État de OPENAI_API_KEY",
            German: "Status von OPENAI_API_KEY",
            Spanish: "Estado de OPENAI_API_KEY",
            Vietnamese: "Trạng thái OPENAI_API_KEY"));
        entries.Add("Setup.Keys", new(
            English: "API keys",
            Italian: "Chiavi API",
            French: "Clés API",
            German: "API-Schlüssel",
            Spanish: "Claves API",
            Vietnamese: "Khóa API"));
        entries.Add("Setup.Language", new(
            English: "Interface language",
            Italian: "Lingua dell'interfaccia",
            French: "Langue",
            German: "Oberflächensprache",
            Spanish: "Idioma",
            Vietnamese: "Ngôn ngữ giao diện"));
        entries.Add("Setup.Location", new(
            English: "Include Windows location in AI requests?",
            Italian: "Includere la posizione Windows nelle richieste AI?",
            French: "Inclure la localisation Windows dans les requêtes IA ?",
            German: "Windows-Standort in KI-Anfragen einbeziehen?",
            Spanish: "¿Incluir la ubicación de Windows en las solicitudes de IA?",
            Vietnamese: "Gửi vị trí Windows trong yêu cầu AI?"));
        entries.Add("Setup.MaxCommandOutput", new(
            English: "Maximum command-output characters kept",
            Italian: "Caratteri massimi output comando conservati",
            French: "Caractères maximaux de sortie de commande conservés",
            German: "Maximal gespeicherte Zeichen der Befehlsausgabe",
            Spanish: "Máximo de caracteres de salida de comando conservados",
            Vietnamese: "Số ký tự đầu ra lệnh tối đa được lưu"));
        entries.Add("Setup.MaxContext", new(
            English: "Maximum context window usage (%)",
            Italian: "Uso massimo context window (%)",
            French: "Utilisation maximale de la fenêtre de contexte (%)",
            German: "Maximale Nutzung des Kontextfensters (%)",
            Spanish: "Uso máximo de la ventana de contexto (%)",
            Vietnamese: "Mức sử dụng cửa sổ ngữ cảnh tối đa (%)"));
        entries.Add("Setup.MaxMessage", new(
            English: "Maximum characters per message",
            Italian: "Caratteri massimi per messaggio",
            French: "Nombre maximal de caractères par message",
            German: "Maximale Zeichen pro Nachricht",
            Spanish: "Máximo de caracteres por mensaje",
            Vietnamese: "Số ký tự tối đa mỗi tin nhắn"));
        entries.Add("Setup.MaxTurns", new(
            English: "Maximum user turns",
            Italian: "Numero massimo di turni utente",
            French: "Nombre maximal de tours utilisateur",
            German: "Maximale Benutzerrunden",
            Spanish: "Máximo de turnos del usuario",
            Vietnamese: "Số lượt người dùng tối đa"));
        entries.Add("Setup.MemoryLimits", new(
            English: "Short-session memory limits",
            Italian: "Limiti memoria per sessioni brevi",
            French: "Limites de mémoire des sessions courtes",
            German: "Speichergrenzen für kurze Sitzungen",
            Spanish: "Límites de memoria para sesiones cortas",
            Vietnamese: "Giới hạn bộ nhớ cho phiên ngắn"));
        entries.Add("Setup.Model", new(
            English: "Model",
            Italian: "Modello",
            French: "Modèle",
            German: "Modell",
            Spanish: "Modelo",
            Vietnamese: "Mô hình"));
        entries.Add("Setup.PreambleCount", new(
            English: "{0:N0} / {1:N0} words · {2:N0} remaining",
            Italian: "{0:N0} / {1:N0} parole · {2:N0} disponibili",
            French: "{0:N0} / {1:N0} mots · {2:N0} restants",
            German: "{0:N0} / {1:N0} Wörter · {2:N0} verbleibend",
            Spanish: "{0:N0} / {1:N0} palabras · {2:N0} restantes",
            Vietnamese: "{0:N0} / {1:N0} từ · còn {2:N0}"));
        entries.Add("Setup.PreambleLimit", new(
            English: "maximum {0:N0} words",
            Italian: "massimo {0:N0} parole",
            French: "maximum {0:N0} mots",
            German: "maximal {0:N0} Wörter",
            Spanish: "máximo {0:N0} palabras",
            Vietnamese: "tối đa {0:N0} từ"));
        entries.Add("Setup.PreambleSecret", new(
            English: "Remove credentials from the preamble before saving it.",
            Italian: "Rimuovi le credenziali dal preambolo prima di salvarlo.",
            French: "Retirez les identifiants secrets du préambule avant de l’enregistrer.",
            German: "Entfernen Sie geheime Zugangsdaten aus der Präambel, bevor Sie sie speichern.",
            Spanish: "Elimina las credenciales del preámbulo antes de guardarlo.",
            Vietnamese: "Hãy xóa thông tin xác thực khỏi lời mở đầu trước khi lưu."));
        entries.Add("Setup.PreambleTooLong", new(
            English: "The preamble contains {0:N0} words. Maximum: {1:N0}.",
            Italian: "Il preambolo contiene {0:N0} parole. Massimo: {1:N0}.",
            French: "Le préambule contient {0:N0} mots. Maximum : {1:N0}.",
            German: "Die Präambel enthält {0:N0} Wörter. Maximum: {1:N0}.",
            Spanish: "El preámbulo contiene {0:N0} palabras. Máximo: {1:N0}.",
            Vietnamese: "Lời mở đầu có {0:N0} từ. Tối đa: {1:N0}."));
        entries.Add("Setup.PreambleUnsafe", new(
            English: "This preamble looks like an attempt to override AI instructions. Rephrase it as a style or response preference.",
            Italian: "Il preambolo sembra tentare di sovrascrivere le istruzioni AI. Riformulalo come preferenza di stile o risposta.",
            French: "Ce préambule semble tenter de remplacer les instructions de l’IA. Reformulez-le comme préférence de style ou de réponse.",
            German: "Diese Präambel scheint KI-Anweisungen überschreiben zu wollen. Formulieren Sie sie als Stil- oder Antwortpräferenz um.",
            Spanish: "Este preámbulo parece intentar sustituir las instrucciones de la IA. Reformúlalo como preferencia de estilo o respuesta.",
            Vietnamese: "Lời mở đầu này có vẻ đang cố ghi đè hướng dẫn AI. Hãy diễn đạt lại thành tùy chọn về phong cách hoặc câu trả lời."));
        entries.Add("Setup.Preferences", new(
            English: "Preferences",
            Italian: "Preferenze",
            French: "Préférences",
            German: "Einstellungen",
            Spanish: "Preferencias",
            Vietnamese: "Tùy chọn"));
        entries.Add("Setup.PromptCaching", new(
            English: "Enable OpenAI prompt caching?",
            Italian: "Abilitare il prompt caching OpenAI?",
            French: "Activer la mise en cache des prompts OpenAI ?",
            German: "OpenAI-Prompt-Caching aktivieren?",
            Spanish: "¿Activar la caché de prompts de OpenAI?",
            Vietnamese: "Bật bộ nhớ đệm prompt của OpenAI?"));
        entries.Add("Setup.RangeError", new(
            English: "Enter a value from {0:N0} to {1:N0}.",
            Italian: "Inserisci un valore compreso tra {0:N0} e {1:N0}.",
            French: "Saisissez une valeur comprise entre {0:N0} et {1:N0}.",
            German: "Geben Sie einen Wert zwischen {0:N0} und {1:N0} ein.",
            Spanish: "Introduce un valor entre {0:N0} y {1:N0}.",
            Vietnamese: "Nhập giá trị từ {0:N0} đến {1:N0}."));
        entries.Add("Setup.Reasoning", new(
            English: "Thinking effort",
            Italian: "Sforzo di ragionamento",
            French: "Effort de raisonnement",
            German: "Denkaufwand",
            Spanish: "Esfuerzo de razonamiento",
            Vietnamese: "Mức suy luận"));
        entries.Add("Setup.Saved", new(
            English: "Configuration saved.",
            Italian: "Configurazione salvata.",
            French: "Configuration enregistrée.",
            German: "Konfiguration gespeichert.",
            Spanish: "Configuración guardada.",
            Vietnamese: "Đã lưu cấu hình."));
        entries.Add("Setup.SetAdminKey", new(
            English: "Set the optional admin key for organization costs?",
            Italian: "Impostare la chiave admin opzionale per i costi organizzazione?",
            French: "Configurer la clé admin facultative pour les coûts d'organisation ?",
            German: "Optionalen Admin-Schlüssel für Organisationskosten setzen?",
            Spanish: "¿Configurar la clave de administrador opcional para los costes de organización?",
            Vietnamese: "Đặt khóa quản trị tùy chọn để đọc chi phí tổ chức?"));
        entries.Add("Setup.SetKey", new(
            English: "Set or change OPENAI_API_KEY now?",
            Italian: "Impostare o cambiare OPENAI_API_KEY adesso?",
            French: "Configurer ou modifier OPENAI_API_KEY maintenant ?",
            German: "OPENAI_API_KEY jetzt setzen oder ändern?",
            Spanish: "¿Configurar o cambiar OPENAI_API_KEY ahora?",
            Vietnamese: "Đặt hoặc đổi OPENAI_API_KEY ngay bây giờ?"));
        entries.Add("Setup.Subtitle", new(
            English: "First-run configuration. Secrets stay in your Windows user environment.",
            Italian: "Configurazione iniziale. I segreti restano nell'ambiente utente Windows.",
            French: "Configuration initiale. Les secrets restent dans l'environnement utilisateur Windows.",
            German: "Erstkonfiguration. Geheimnisse bleiben in der Windows-Benutzerumgebung.",
            Spanish: "Configuración inicial. Los secretos permanecen en el entorno de usuario de Windows.",
            Vietnamese: "Cấu hình lần đầu. Khóa bí mật chỉ nằm trong môi trường người dùng Windows."));
        entries.Add("Setup.Summary", new(
            English: "Configuration summary",
            Italian: "Riepilogo configurazione",
            French: "Résumé de configuration",
            German: "Konfigurationsübersicht",
            Spanish: "Resumen de configuración",
            Vietnamese: "Tóm tắt cấu hình"));
        entries.Add("Setup.Test", new(
            English: "Test the AI connection now?",
            Italian: "Verificare ora la connessione AI?",
            French: "Tester la connexion IA maintenant ?",
            German: "KI-Verbindung jetzt testen?",
            Spanish: "¿Probar ahora la conexión de IA?",
            Vietnamese: "Kiểm tra kết nối AI ngay?"));
        entries.Add("Setup.Title", new(
            English: "AI SETUP // PMU-SETUP-01",
            Italian: "CONFIGURAZIONE AI // PMU-SETUP-01",
            French: "CONFIGURATION IA // PMU-SETUP-01",
            German: "KI-EINRICHTUNG // PMU-SETUP-01",
            Spanish: "CONFIGURACIÓN DE IA // PMU-SETUP-01",
            Vietnamese: "THIẾT LẬP AI // PMU-SETUP-01"));
    }
}
