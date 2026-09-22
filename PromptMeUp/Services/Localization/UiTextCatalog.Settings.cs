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
        entries.Add("Settings.OverviewScroll", new(
            English: "Ctrl+↑/↓: content",
            Italian: "Ctrl+↑/↓: contenuto",
            French: "Ctrl+↑/↓: contenu",
            German: "Ctrl+↑/↓: Inhalt",
            Spanish: "Ctrl+↑/↓: contenido",
            Vietnamese: "Ctrl+↑/↓: nội dung"));
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
        entries.Add("Settings.SessionSummary", new(
            English: "Show session summary during work",
            Italian: "Mostra lo specchietto sessione durante il lavoro",
            French: "Afficher le résumé de session pendant le travail",
            German: "Sitzungsübersicht während der Arbeit anzeigen",
            Spanish: "Mostrar el resumen de sesión durante el trabajo",
            Vietnamese: "Hiện bảng tóm tắt phiên khi đang làm việc"));
        entries.Add("Settings.SessionSummaryHelp", new(
            English: "When off, the summary appears only when a workflow ends. /status, /context, and an 80% context warning always show it.",
            Italian: "Se disattivo, lo specchietto compare solo al termine del flusso. /status, /context e un avviso al 80% del contesto lo mostrano sempre.",
            French: "Lorsqu'il est désactivé, le résumé apparaît seulement à la fin d'un flux. /status, /context et un avertissement à 80 % du contexte l'affichent toujours.",
            German: "Wenn ausgeschaltet, erscheint die Übersicht nur am Ende eines Ablaufs. /status, /context und eine Kontextwarnung bei 80 % zeigen sie immer.",
            Spanish: "Al desactivarlo, el resumen aparece solo al terminar un flujo. /status, /context y un aviso al 80 % del contexto siempre lo muestran.",
            Vietnamese: "Khi tắt, bảng tóm tắt chỉ hiện khi quy trình kết thúc. /status, /context và cảnh báo ngữ cảnh 80% luôn hiển thị nó."));
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
        entries.Add("Settings.Skills", new("Skills", "Skills", "Compétences", "Skills", "Skills", "Kỹ năng"));
        entries.Add("Settings.Learning", new(
            "Memory",
            "Memoria",
            "Mémoire",
            "Gedächtnis",
            "Memoria",
            "Bộ nhớ"));
        entries.Add("Settings.Privacy", new("Privacy", "Privacy", "Confidentialité", "Datenschutz", "Privacidad", "Quyền riêng tư"));
        entries.Add("Settings.DraftStatus", new(
            "Save applies your changes",
            "Salva applica le modifiche",
            "Enregistrer applique vos changements",
            "Speichern übernimmt deine Änderungen",
            "Guardar aplica tus cambios",
            "Lưu để áp dụng thay đổi"));
        entries.Add("Settings.FeaturesTitle", new(
            "Global features",
            "Funzioni globali",
            "Fonctions globales",
            "Globale Funktionen",
            "Funciones globales",
            "Tính năng toàn cục"));
        entries.Add("Settings.FeaturesGate", new(
            "These options work when global Skills and memory is on.",
            "Queste opzioni funzionano quando Skills e memoria globale è attivo.",
            "Ces options fonctionnent lorsque Compétences et mémoire globales sont activées.",
            "Diese Optionen gelten, wenn globale Skills und Gedächtnis aktiv sind.",
            "Estas opciones funcionan cuando Skills y memoria globales están activos.",
            "Các tùy chọn này hoạt động khi bật Kỹ năng và bộ nhớ toàn cục."));
        entries.Add("Settings.SkillCatalogUnavailable", new(
            "Cannot read the skills. Check imported skills; you can still change other settings.",
            "Non riesco a leggere le skill. Controlla quelle importate; puoi comunque cambiare le altre impostazioni.",
            "Impossible de lire les compétences. Vérifiez celles importées ; vous pouvez modifier les autres réglages.",
            "Die Skills lassen sich nicht lesen. Prüfe importierte Skills; andere Einstellungen bleiben verfügbar.",
            "No se pueden leer las skills. Revisa las importadas; puedes cambiar los demás ajustes.",
            "Không đọc được kỹ năng. Hãy kiểm tra kỹ năng đã nhập; bạn vẫn có thể đổi cài đặt khác."));
        entries.Add("Settings.FeatureSkillsAndMemory", new(
            "Skills and memory",
            "Skills e memoria",
            "Compétences et mémoire",
            "Skills und Gedächtnis",
            "Skills y memoria",
            "Kỹ năng và bộ nhớ"));
        entries.Add("Settings.FeatureSkills", new(
            "Active skills",
            "Skill attive",
            "Compétences actives",
            "Aktive Skills",
            "Skills activas",
            "Kỹ năng đang hoạt động"));
        entries.Add("Settings.FeatureSkillCount", new("{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}"));
        entries.Add("Settings.FeatureAutomatic", new(
            "Choose skills based on the question",
            "Scegli le skill in base alla domanda",
            "Choisir les compétences selon la question",
            "Skills passend zur Frage auswählen",
            "Elegir skills según la pregunta",
            "Chọn kỹ năng phù hợp với câu hỏi"));
        entries.Add("Settings.FeatureCapture", new(
            "Keep my messages",
            "Conserva i miei messaggi",
            "Conserver mes messages",
            "Meine Nachrichten speichern",
            "Guardar mis mensajes",
            "Giữ tin nhắn của tôi"));
        entries.Add("Settings.FeatureReminder", new(
            "Remind me to review memories",
            "Promemoria per rivedere i ricordi",
            "Me rappeler de vérifier les souvenirs",
            "An die Prüfung von Erinnerungen erinnern",
            "Recordarme revisar los recuerdos",
            "Nhắc tôi kiểm tra ghi nhớ"));
        entries.Add("Settings.SkillsHelp", new(
            "Skills help with tasks such as reading files or searching the web. Choose which ones you want to use; actions still need your approval.",
            "Le skill aiutano con attività come leggere file o cercare sul web. Scegli quelle da usare; le azioni richiedono comunque la tua conferma.",
            "Les compétences aident à lire des fichiers ou chercher sur le web. Choisissez celles à utiliser ; les actions nécessitent toujours votre accord.",
            "Skills helfen etwa beim Lesen von Dateien oder bei der Websuche. Wähle die gewünschten Skills; Aktionen brauchen weiterhin deine Zustimmung.",
            "Las skills ayudan a leer archivos o buscar en la web. Elige cuáles usar; las acciones siguen requiriendo tu aprobación.",
            "Kỹ năng giúp đọc tệp hoặc tìm trên web. Chọn kỹ năng muốn dùng; thao tác vẫn cần bạn xác nhận."));
        entries.Add("Settings.LearningHelp", new(
            "Keep your messages so Dream can suggest useful memories, such as «I prefer short answers». You decide which memories to save.",
            "Conserva i tuoi messaggi perché Dream suggerisca ricordi utili, come «preferisco risposte brevi». Decidi tu quali salvare.",
            "Conservez vos messages pour que Dream suggère des souvenirs utiles, comme «je préfère les réponses courtes». Vous décidez lesquels enregistrer.",
            "Behalte deine Nachrichten, damit Dream nützliche Erinnerungen vorschlagen kann, etwa «Ich bevorzuge kurze Antworten». Du entscheidest, was gespeichert wird.",
            "Conserva tus mensajes para que Dream sugiera recuerdos útiles, como «prefiero respuestas breves». Tú decides cuáles guardar.",
            "Giữ tin nhắn để Dream đề xuất ghi nhớ hữu ích, như «tôi thích câu trả lời ngắn». Bạn quyết định ghi nhớ nào được lưu."));
        entries.Add("Settings.FeatureMenuNotice", new(
            "Memories save separately: Cancel in Settings does not undo them.",
            "I ricordi si salvano a parte: Annulla nelle impostazioni non li ripristina.",
            "Les souvenirs sont enregistrés à part : annuler les réglages ne les annule pas.",
            "Erinnerungen werden separat gespeichert: Abbrechen in Einstellungen macht sie nicht rückgängig.",
            "Los recuerdos se guardan aparte: Cancelar en Configuración no los deshace.",
            "Ghi nhớ được lưu riêng: Hủy trong Cài đặt không hoàn tác chúng."));
        entries.Add("Settings.PrivacyHelp", new(
            "Where your data stays and when it leaves your computer.",
            "Dove restano i tuoi dati e quando escono dal computer.",
            "Où restent vos données et quand elles quittent votre appareil.",
            "Wo deine Daten bleiben und wann sie dein Gerät verlassen.",
            "Dónde quedan tus datos y cuándo salen de tu equipo.",
            "Dữ liệu được lưu ở đâu và khi nào rời khỏi máy của bạn."));
        entries.Add("Settings.PrivacyLocal", new(
            "On your computer",
            "Sul tuo computer",
            "Sur votre appareil",
            "Auf deinem Gerät",
            "En tu equipo",
            "Trên máy của bạn"));
        entries.Add("Settings.PrivacyLocalInfo", new(
            "Settings, history, memories and screenshots are saved on your computer. API keys are separate, not in settings or the database.",
            "Impostazioni, cronologia, ricordi e screenshot sono salvati sul computer. Le chiavi API sono separate, non nelle impostazioni o nel database.",
            "Réglages, historique, souvenirs et captures sont enregistrés sur votre appareil. Les clés API sont séparées, ni dans les réglages ni dans la base de données.",
            "Einstellungen, Verlauf, Erinnerungen und Bildschirmfotos werden auf deinem Gerät gespeichert. API-Schlüssel liegen separat, nicht in Einstellungen oder Datenbank.",
            "Ajustes, historial, recuerdos y capturas se guardan en tu equipo. Las claves API van aparte, no en los ajustes ni en la base de datos.",
            "Cài đặt, lịch sử, ghi nhớ và ảnh chụp được lưu trên máy. Khóa API được giữ riêng, không trong cài đặt hay cơ sở dữ liệu."));
        entries.Add("Settings.PrivacyProvider", new(
            "Sent to OpenAI",
            "Inviato a OpenAI",
            "Envoyé à OpenAI",
            "An OpenAI gesendet",
            "Enviado a OpenAI",
            "Gửi đến OpenAI"));
        entries.Add("Settings.PrivacyProviderInfo", new(
            "OpenAI receives your question, recent chat, relevant memories, selected skill instructions and your name if set. Secret filtering can miss things: avoid entering private credentials.",
            "OpenAI riceve domanda, chat recente, ricordi utili, istruzioni delle skill scelte e nome, se impostato. Il filtro può non riconoscere tutti i segreti: evita di inserirli.",
            "OpenAI reçoit votre question, le chat récent, les souvenirs utiles, les instructions des compétences choisies et votre nom si renseigné. Le filtre peut manquer des secrets : évitez de saisir des identifiants confidentiels.",
            "OpenAI erhält deine Frage, den jüngsten Chat, passende Erinnerungen, ausgewählte Skill-Anweisungen und deinen Namen, falls angegeben. Der Filter erkennt nicht jedes Geheimnis: Gib keine vertraulichen Zugangsdaten ein.",
            "OpenAI recibe tu pregunta, el chat reciente, los recuerdos útiles, las instrucciones de las skills elegidas y tu nombre si lo indicaste. El filtro puede pasar por alto secretos: evita introducirlos.",
            "OpenAI nhận câu hỏi, trò chuyện gần đây, ghi nhớ liên quan, hướng dẫn kỹ năng đã chọn và tên nếu bạn đã đặt. Bộ lọc có thể bỏ sót bí mật: tránh nhập thông tin xác thực riêng tư."));
        entries.Add("Settings.PrivacyLearning", new(
            "Messages for new memories",
            "Messaggi per nuovi ricordi",
            "Messages pour de nouveaux souvenirs",
            "Nachrichten für neue Erinnerungen",
            "Mensajes para nuevos recuerdos",
            "Tin nhắn để tạo ghi nhớ mới"));
        entries.Add("Settings.PrivacyLearningInfo", new(
            "You can choose to keep your messages for memory suggestions. Dream and AI memory review ask for confirmation before sending data to OpenAI.",
            "Puoi scegliere di conservare i tuoi messaggi per suggerire ricordi. Dream e la revisione AI dei ricordi chiedono conferma prima dell’invio a OpenAI.",
            "Vous pouvez choisir de conserver vos messages pour suggérer des souvenirs. Dream et l’examen IA des souvenirs demandent confirmation avant l’envoi à OpenAI.",
            "Du kannst deine Nachrichten für Erinnerungsvorschläge behalten lassen. Dream und die KI-Prüfung von Erinnerungen fragen vor dem Senden an OpenAI nach.",
            "Puedes elegir conservar tus mensajes para sugerir recuerdos. Dream y la revisión de recuerdos con IA piden confirmación antes de enviar datos a OpenAI.",
            "Bạn có thể chọn giữ tin nhắn để nhận đề xuất ghi nhớ. Dream và tính năng kiểm tra ghi nhớ bằng AI yêu cầu xác nhận trước khi gửi dữ liệu tới OpenAI."));
        entries.Add("Settings.PrivacySkills", new(
            "Actions and the web",
            "Azioni e accesso al web",
            "Actions et accès au web",
            "Aktionen und Webzugriff",
            "Acciones y acceso a la web",
            "Thao tác và truy cập web"));
        entries.Add("Settings.PrivacySkillsInfo", new(
            "Actions require approval; optional AI review sends command data to OpenAI before execution approval. Web actions contact the chosen site or DuckDuckGo; skill results are not automatically sent to OpenAI.",
            "Le azioni richiedono conferma; il controllo AI facoltativo invia il comando e i suoi dati a OpenAI prima della tua conferma. Le azioni web contattano il sito scelto o DuckDuckGo; i risultati delle skill non vanno automaticamente a OpenAI.",
            "Les actions nécessitent votre accord ; l’examen IA facultatif envoie la commande à OpenAI avant l’autorisation d’exécution. Les actions web contactent le site choisi ou DuckDuckGo ; leurs résultats ne vont pas automatiquement à OpenAI.",
            "Aktionen brauchen deine Zustimmung; die optionale KI-Prüfung sendet Befehlsdaten vor der Ausführungsfreigabe an OpenAI. Webaktionen kontaktieren die gewählte Seite oder DuckDuckGo; Skill-Ergebnisse gehen nicht automatisch an OpenAI.",
            "Las acciones requieren aprobación; la revisión IA opcional envía el comando a OpenAI antes de autorizar su ejecución. Las acciones web contactan el sitio elegido o DuckDuckGo; sus resultados no se envían automáticamente a OpenAI.",
            "Thao tác cần được duyệt; kiểm tra AI tùy chọn gửi dữ liệu lệnh tới OpenAI trước khi bạn duyệt chạy. Thao tác web liên hệ trang đã chọn hoặc DuckDuckGo; kết quả kỹ năng không tự gửi tới OpenAI."));
        entries.Add("Settings.PrivacyControl", new(
            "What gets deleted",
            "Cosa viene cancellato",
            "Ce qui est supprimé",
            "Was gelöscht wird",
            "Qué se borra",
            "Nội dung bị xóa"));
        entries.Add("Settings.PrivacyControlInfo", new(
            "Turning collection off deletes collected messages and suggestions, not saved memories or history. It does not remove copies already sent to OpenAI.",
            "Spegnere la raccolta cancella messaggi raccolti e suggerimenti, non ricordi salvati o cronologia. Non rimuove le copie già inviate a OpenAI.",
            "Arrêter la collecte supprime messages collectés et suggestions, pas les souvenirs enregistrés ni l’historique. Cela ne retire pas les copies déjà envoyées à OpenAI.",
            "Ausschalten der Erfassung löscht erfasste Nachrichten und Vorschläge, nicht gespeicherte Erinnerungen oder den Verlauf. Bereits an OpenAI gesendete Kopien bleiben bestehen.",
            "Desactivar la recopilación borra mensajes recopilados y sugerencias, no recuerdos guardados ni historial. No elimina las copias ya enviadas a OpenAI.",
            "Tắt thu thập xóa tin nhắn đã thu thập và đề xuất, không xóa ghi nhớ đã lưu hay lịch sử. Việc này không xóa bản sao đã gửi tới OpenAI."));
        entries.Add("Settings.FeaturesDraftHelp", new(
            "Save applies these changes; Cancel discards them.",
            "Salva applica queste modifiche; Annulla le scarta.",
            "Enregistrer applique ces changements ; Annuler les abandonne.",
            "Speichern übernimmt diese Änderungen; Abbrechen verwirft sie.",
            "Guardar aplica estos cambios; Cancelar los descarta.",
            "Lưu áp dụng thay đổi; Hủy bỏ qua chúng."));
        entries.Add("Settings.FeaturesPartiallySaved", new(
            "Skills and memory were saved. The other settings could not be saved.",
            "Skills e memoria salvate. Non è stato possibile salvare le altre impostazioni.",
            "Les compétences et la mémoire ont été enregistrées. Les autres réglages n’ont pas pu être enregistrés.",
            "Skills und Gedächtnis wurden gespeichert. Die anderen Einstellungen konnten nicht gespeichert werden.",
            "Skills y memoria se guardaron. No se pudieron guardar los demás ajustes.",
            "Đã lưu kỹ năng và bộ nhớ. Không thể lưu các cài đặt khác."));
        entries.Add("Settings.FeatureMaster", new(
            "Global skills and memory",
            "Skills e memoria globali",
            "Compétences et mémoire globales",
            "Globale Skills und Gedächtnis",
            "Skills y memoria globales",
            "Kỹ năng và bộ nhớ toàn cục"));
        entries.Add("Settings.FeatureMasterHelp", new(
            "Turns on global skill and memory options. Message collection stays off until you choose it separately.",
            "Attiva le opzioni globali di skills e memoria. I messaggi non vengono raccolti finché non scegli di farlo a parte.",
            "Active les options globales de compétences et mémoire. La collecte de messages reste désactivée tant que vous ne la choisissez pas séparément.",
            "Aktiviert globale Skill- und Gedächtnisoptionen. Nachrichten werden erst erfasst, wenn du dies separat auswählst.",
            "Activa las opciones globales de skills y memoria. Los mensajes no se recopilan hasta que lo elijas por separado.",
            "Bật các tùy chọn kỹ năng và bộ nhớ toàn cục. Tin nhắn chỉ được thu thập khi bạn bật riêng tùy chọn đó."));
        entries.Add("Settings.FeatureEnableFirst", new(
            "First turn on global Skills and memory.",
            "Prima attiva Skills e memoria globale.",
            "Activez d’abord Compétences et mémoire globales.",
            "Aktiviere zuerst globale Skills und Gedächtnis.",
            "Primero activa Skills y memoria globales.",
            "Trước tiên bật Kỹ năng và bộ nhớ toàn cục."));
        entries.Add("Settings.CaptureConsent", new(
            "I agree to keep my messages",
            "Accetto di conservare i messaggi",
            "J’accepte de conserver mes messages",
            "Speichern meiner Nachrichten erlauben",
            "Acepto guardar mis mensajes",
            "Tôi đồng ý giữ tin nhắn"));
        entries.Add("Settings.ClearLearningConsent", new(
            "I confirm deletion",
            "Confermo la cancellazione",
            "Je confirme la suppression",
            "Löschung bestätigen",
            "Confirmo el borrado",
            "Tôi xác nhận xóa"));
        entries.Add("Settings.FeatureConsentRequired", new(
            "Confirm the requested collection or deletion before saving.",
            "Conferma la raccolta o la cancellazione richiesta prima di salvare.",
            "Confirmez la collecte ou la suppression demandée avant d’enregistrer.",
            "Bestätige die gewünschte Erfassung oder Löschung vor dem Speichern.",
            "Confirma la recopilación o el borrado solicitado antes de guardar.",
            "Xác nhận việc thu thập hoặc xóa được yêu cầu trước khi lưu."));
        entries.Add("Settings.SkillApprovalHelp", new(
            "Read this skill’s instructions and scripts below; use Ctrl+↑/↓ to scroll. Turning it on approves these exact files, not automatic actions.",
            "Leggi qui sotto istruzioni e script della skill; scorri con Ctrl+↑/↓. Attivarla approva questi file, non l’esecuzione automatica di azioni.",
            "Lisez les instructions et scripts ci-dessous ; faites défiler avec Ctrl+↑/↓. L’activation approuve ces fichiers précis, pas des actions automatiques.",
            "Lies die Anweisungen und Skripte unten; scrolle mit Strg+↑/↓. Aktivieren genehmigt genau diese Dateien, keine automatischen Aktionen.",
            "Lee las instrucciones y los scripts de abajo; desplázate con Ctrl+↑/↓. Activarla aprueba estos archivos exactos, no acciones automáticas.",
            "Đọc hướng dẫn và mã lệnh bên dưới; dùng Ctrl+↑/↓ để cuộn. Bật kỹ năng là duyệt đúng các tệp này, không cho phép thao tác tự chạy."));
        entries.Add("Settings.SavedMemoriesNotice", new(
            "Memories save separately: Cancel in Settings does not undo them.",
            "I ricordi si salvano a parte: Annulla nelle impostazioni non li ripristina.",
            "Les souvenirs sont enregistrés à part : annuler les réglages ne les annule pas.",
            "Erinnerungen werden separat gespeichert: Abbrechen in Einstellungen macht sie nicht rückgängig.",
            "Los recuerdos se guardan aparte: Cancelar en Configuración no los deshace.",
            "Ghi nhớ được lưu riêng: Hủy trong Cài đặt không hoàn tác chúng."));
        entries.Add("Settings.DisableFeaturesConsentInfo", new(
            "Saving this choice turns off global skills and memory and permanently deletes collected messages and suggestions. Saved memories and earlier history remain.",
            "Salvare questa scelta disattiva skills e memoria globale e cancella definitivamente messaggi raccolti e suggerimenti. I ricordi salvati e la cronologia precedente restano.",
            "Enregistrer ce choix désactive les compétences et mémoire globales et supprime définitivement messages collectés et suggestions. Les souvenirs enregistrés et l’historique précédent restent.",
            "Diese Wahl zu speichern deaktiviert globale Skills und Gedächtnis und löscht erfasste Nachrichten und Vorschläge endgültig. Gespeicherte Erinnerungen und der frühere Verlauf bleiben.",
            "Guardar esta opción desactiva skills y memoria globales y borra definitivamente mensajes recopilados y sugerencias. Los recuerdos guardados y el historial anterior permanecen.",
            "Lưu lựa chọn này sẽ tắt kỹ năng và bộ nhớ toàn cục, đồng thời xóa vĩnh viễn tin nhắn đã thu thập và đề xuất. Ghi nhớ đã lưu và lịch sử trước đó vẫn còn."));
    }
}
