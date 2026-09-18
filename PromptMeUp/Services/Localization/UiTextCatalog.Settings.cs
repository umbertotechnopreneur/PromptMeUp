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
            English: "Ctrl+↑/↓: overview",
            Italian: "Ctrl+↑/↓: specchietto",
            French: "Ctrl+↑/↓: aperçu",
            German: "Ctrl+↑/↓: Übersicht",
            Spanish: "Ctrl+↑/↓: resumen",
            Vietnamese: "Ctrl+↑/↓: tổng quan"));
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
        entries.Add("Settings.Skills", new("Skills", "Skills", "Compétences", "Skills", "Skills", "Kỹ năng"));
        entries.Add("Settings.Learning", new("Learning", "Apprendimento", "Apprentissage", "Lernen", "Aprendizaje", "Học hỏi"));
        entries.Add("Settings.Privacy", new("Privacy", "Privacy", "Confidentialité", "Datenschutz", "Privacidad", "Quyền riêng tư"));
        entries.Add("Settings.DraftStatus", new(
            "Settings draft · apply with Save", "Bozza impostazioni · applica con Salva", "Brouillon des réglages · appliquer avec Enregistrer",
            "Einstellungsentwurf · mit Speichern anwenden", "Borrador de ajustes · aplicar con Guardar", "Bản nháp cài đặt · áp dụng bằng Lưu"));
        entries.Add("Settings.FeaturesTitle", new(
            "Current project · feature status", "Progetto corrente · stato delle funzionalità", "Projet actuel · état des fonctionnalités",
            "Aktuelles Projekt · Funktionsstatus", "Proyecto actual · estado de las funciones", "Dự án hiện tại · trạng thái tính năng"));
        entries.Add("Settings.FeaturesGate", new(
            "Saved options apply only while the experiment is enabled.", "Le opzioni salvate si applicano solo mentre l’esperimento è abilitato.",
            "Les options enregistrées s’appliquent uniquement lorsque l’expérience est activée.", "Gespeicherte Optionen gelten nur bei aktiviertem Experiment.",
            "Las opciones guardadas solo se aplican mientras el experimento está activado.", "Tùy chọn đã lưu chỉ áp dụng khi thử nghiệm được bật."));
        entries.Add("Settings.SkillCatalogUnavailable", new(
            "Skill catalog unavailable. Check imported packages; other settings remain accessible.",
            "Catalogo skills non disponibile. Controlla i pacchetti importati; le altre impostazioni restano accessibili.",
            "Catalogue de compétences indisponible. Vérifiez les paquets importés ; les autres réglages restent accessibles.",
            "Skill-Katalog nicht verfügbar. Prüfe importierte Pakete; andere Einstellungen bleiben zugänglich.",
            "Catálogo de skills no disponible. Revisa los paquetes importados; los demás ajustes siguen accesibles.",
            "Danh mục kỹ năng không khả dụng. Kiểm tra các gói đã nhập; vẫn có thể mở các cài đặt khác."));
        entries.Add("Settings.FeatureExperiment", new("Experiment", "Esperimento", "Expérience", "Experiment", "Experimento", "Thử nghiệm"));
        entries.Add("Settings.FeatureSkills", new("Enabled skills", "Skills abilitate", "Compétences activées", "Aktivierte Skills", "Skills habilitadas", "Kỹ năng đã bật"));
        entries.Add("Settings.FeatureSkillCount", new("{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}", "{0:N0} / {1:N0}"));
        entries.Add("Settings.FeatureAutomatic", new("Contextual selection", "Selezione contestuale", "Sélection contextuelle", "Kontextuelle Auswahl", "Selección contextual", "Chọn theo ngữ cảnh"));
        entries.Add("Settings.FeatureCapture", new("Learning capture", "Raccolta per apprendimento", "Collecte pour l’apprentissage", "Lerndatenerfassung", "Captura para aprendizaje", "Thu thập dữ liệu học"));
        entries.Add("Settings.FeatureReminder", new("Maintenance reminder", "Promemoria manutenzione", "Rappel de maintenance", "Wartungshinweis", "Recordatorio de mantenimiento", "Nhắc bảo trì"));
        entries.Add("Settings.SkillsHelp", new(
            "Open the existing skills menu to inspect, enable or run a package. Visiting this section runs nothing.",
            "Apri il menu skills esistente per esaminare, abilitare o eseguire un pacchetto. Visitare questa sezione non esegue nulla.",
            "Ouvrez le menu des compétences pour examiner, activer ou lancer un paquet. Consulter cette section ne lance rien.",
            "Öffne das bestehende Skill-Menü zum Prüfen, Aktivieren oder Ausführen eines Pakets. Der Abschnitt allein führt nichts aus.",
            "Abre el menú de skills para revisar, habilitar o ejecutar un paquete. Visitar esta sección no ejecuta nada.",
            "Mở menu kỹ năng hiện có để xem, bật hoặc chạy gói. Chỉ xem phần này không chạy gì."));
        entries.Add("Settings.LearningHelp", new(
            "Open learning preferences to manage capture, inspect observations or choose a maintenance reminder. Dream and heartbeat still require explicit review.",
            "Apri le preferenze di apprendimento per gestire la raccolta, leggere le osservazioni o scegliere il promemoria. Dream e heartbeat richiedono sempre una revisione esplicita.",
            "Ouvrez les préférences d’apprentissage pour gérer la collecte, lire les observations ou choisir un rappel. Dream et heartbeat exigent toujours un examen explicite.",
            "Öffne die Lerneinstellungen für Erfassung, Beobachtungen und Wartungshinweis. Dream und Heartbeat erfordern weiterhin ausdrückliche Prüfung.",
            "Abre las preferencias de aprendizaje para gestionar la captura, leer observaciones o elegir un recordatorio. Dream y heartbeat siguen requiriendo revisión explícita.",
            "Mở tùy chọn học để quản lý thu thập, xem quan sát hoặc chọn nhắc bảo trì. Dream và heartbeat vẫn cần xem xét rõ ràng."));
        entries.Add("Settings.FeatureMenuNotice", new(
            "Changes in Skills, Learning and Memories save immediately; cancelling Settings does not undo them. These menus use the saved AI, model and credentials until you save the main settings.",
            "Le modifiche in Skills, Apprendimento e Memorie si salvano subito; Annulla nelle Impostazioni non le annulla. Questi menu usano AI, modello e credenziali già salvati finché non salvi le impostazioni principali.",
            "Les changements dans Compétences, Apprentissage et Mémoires sont immédiats ; annuler les réglages ne les annule pas. Ces menus utilisent l’IA, le modèle et les identifiants enregistrés jusqu’à l’enregistrement des réglages.",
            "Änderungen in Skills, Lernen und Erinnerungen werden sofort gespeichert; Abbrechen in Einstellungen macht sie nicht rückgängig. Die Menüs nutzen gespeicherte KI-, Modell- und Zugangsdaten bis zum Speichern der Haupteinstellungen.",
            "Los cambios en Skills, Aprendizaje y Memorias se guardan al instante; cancelar Ajustes no los deshace. Estos menús usan la IA, el modelo y las credenciales guardados hasta guardar los ajustes principales.",
            "Thay đổi trong Kỹ năng, Học hỏi và Ghi nhớ được lưu ngay; hủy Cài đặt không hoàn tác chúng. Các menu dùng AI, mô hình và thông tin xác thực đã lưu cho đến khi lưu cài đặt chính."));
        entries.Add("Settings.PrivacyHelp", new(
            "Read-only privacy summary. Feature states are saved settings, not this form’s draft.",
            "Riepilogo privacy in sola lettura. Gli stati delle funzionalità sono salvati, non fanno parte di questa bozza.",
            "Résumé de confidentialité en lecture seule. Les états des fonctionnalités sont enregistrés, pas ceux du brouillon.",
            "Datenschutzübersicht nur zum Lesen. Funktionszustände sind gespeichert, nicht Teil dieses Entwurfs.",
            "Resumen de privacidad de solo lectura. Los estados de las funciones son los guardados, no los del borrador.",
            "Tóm tắt quyền riêng tư chỉ đọc. Trạng thái tính năng đã được lưu, không phải bản nháp này."));
        entries.Add("Settings.PrivacyLocal", new("On your machine", "Sul tuo computer", "Sur votre appareil", "Auf deinem Gerät", "En tu equipo", "Trên máy của bạn"));
        entries.Add("Settings.PrivacyLocalInfo", new(
            "Settings, saved notes, learning evidence and activity history stay in local storage. API keys are not saved in settings or the database.",
            "Impostazioni, note salvate, evidenze di apprendimento e cronologia attività restano nell’archivio locale. Le chiavi API non vengono salvate nelle impostazioni o nel database.",
            "Réglages, notes, preuves d’apprentissage et historique d’activité restent en stockage local. Les clés API ne sont pas enregistrées dans les réglages ni la base.",
            "Einstellungen, Notizen, Lernbelege und Aktivitätsverlauf bleiben lokal gespeichert. API-Schlüssel stehen weder in Einstellungen noch Datenbank.",
            "Ajustes, notas, evidencias de aprendizaje e historial de actividad quedan en almacenamiento local. Las claves API no se guardan en ajustes ni en la base de datos.",
            "Cài đặt, ghi chú, bằng chứng học và lịch sử hoạt động được lưu cục bộ. Khóa API không lưu trong cài đặt hoặc cơ sở dữ liệu."));
        entries.Add("Settings.PrivacyProvider", new("Sent to OpenAI", "Inviato a OpenAI", "Envoyé à OpenAI", "An OpenAI gesendet", "Enviado a OpenAI", "Gửi đến OpenAI"));
        entries.Add("Settings.PrivacyProviderInfo", new(
            "Questions include retained chat, selected notes and skills, and an optional preferred name. Optional AI command review sends command data before execution approval. Secret filtering is not a guarantee.",
            "Le domande includono chat conservata, note e skills selezionate e l’eventuale nome preferito. La revisione AI facoltativa invia dati del comando prima dell’approvazione. Il filtro dei segreti non è una garanzia.",
            "Les questions incluent le chat conservé, notes et compétences choisies, et le nom facultatif. La revue IA facultative envoie la commande avant l’autorisation d’exécution. Le filtrage des secrets n’est pas une garantie.",
            "Fragen enthalten behaltenen Chat, ausgewählte Notizen und Skills sowie den optionalen Namen. Optionale KI-Prüfung sendet Befehlsdaten vor der Ausführungsfreigabe. Geheimnisfilter bieten keine Garantie.",
            "Las preguntas incluyen chat conservado, notas y skills seleccionadas y el nombre opcional. La revisión IA opcional envía datos del comando antes de autorizarlo. El filtro de secretos no es una garantía.",
            "Câu hỏi gồm chat được giữ, ghi chú và kỹ năng đã chọn, cùng tên tùy chọn. Kiểm tra lệnh bằng AI có thể gửi dữ liệu lệnh trước khi duyệt chạy. Bộ lọc bí mật không bảo đảm tuyệt đối."));
        entries.Add("Settings.PrivacyLearning", new("Learning retention", "Durata apprendimento", "Durée de conservation", "Aufbewahrung von Lerndaten", "Retención del aprendizaje", "Lưu giữ dữ liệu học"));
        entries.Add("Settings.PrivacyLearningInfo", new(
            "Opt-in capture keeps up to 200 observations per project; entries older than 30 days are removed on the next learning access. Turning it off clears observations and proposals, not approved memories or earlier history. Dream and AI heartbeat ask before sharing.",
            "La raccolta facoltativa conserva fino a 200 osservazioni per progetto; quelle oltre 30 giorni vengono rimosse al prossimo accesso all’apprendimento. Disabilitarla cancella osservazioni e proposte, non memorie approvate o cronologia precedente. Dream e heartbeat AI chiedono prima dell’invio.",
            "La collecte volontaire garde jusqu’à 200 observations par projet ; celles de plus de 30 jours sont supprimées au prochain accès à l’apprentissage. La désactiver efface observations et propositions, pas les mémoires approuvées ni l’historique. Dream et heartbeat IA demandent avant l’envoi.",
            "Optionale Erfassung behält bis zu 200 Beobachtungen je Projekt; Einträge über 30 Tage werden beim nächsten Lernzugriff entfernt. Ausschalten löscht Beobachtungen und Vorschläge, nicht genehmigte Erinnerungen oder früheren Verlauf. Dream und KI-Heartbeat fragen vor dem Teilen.",
            "La captura voluntaria guarda hasta 200 observaciones por proyecto; las de más de 30 días se eliminan al próximo acceso al aprendizaje. Desactivarla borra observaciones y propuestas, no memorias aprobadas ni historial previo. Dream y heartbeat IA preguntan antes de enviar.",
            "Thu thập tự chọn giữ tối đa 200 quan sát mỗi dự án; mục quá 30 ngày được xóa khi truy cập dữ liệu học lần tới. Tắt sẽ xóa quan sát và đề xuất, không xóa ghi nhớ đã duyệt hay lịch sử cũ. Dream và heartbeat AI hỏi trước khi gửi."));
        entries.Add("Settings.PrivacySkills", new("Skill actions", "Azioni delle skills", "Actions des compétences", "Skill-Aktionen", "Acciones de skills", "Thao tác kỹ năng"));
        entries.Add("Settings.PrivacySkillsInfo", new(
            "Actions need approval. HTTP and web search contact the chosen site or DuckDuckGo. Screenshots stay local; images cannot be redacted. Skill output is not automatically sent to OpenAI.",
            "Le azioni richiedono approvazione. HTTP e ricerca web contattano il sito scelto o DuckDuckGo. Gli screenshot restano locali; le immagini non possono essere oscurate. L’output delle skills non va automaticamente a OpenAI.",
            "Les actions exigent une approbation. HTTP et recherche web contactent le site choisi ou DuckDuckGo. Les captures restent locales ; les images ne sont pas expurgées. Aucune sortie de compétence n’est envoyée automatiquement à OpenAI.",
            "Aktionen brauchen Freigabe. HTTP und Websuche kontaktieren die gewählte Seite oder DuckDuckGo. Bildschirmfotos bleiben lokal; Bilder können nicht bereinigt werden. Skill-Ausgabe geht nicht automatisch an OpenAI.",
            "Las acciones requieren aprobación. HTTP y búsqueda web contactan el sitio elegido o DuckDuckGo. Las capturas quedan locales; las imágenes no se censuran. La salida de skills no se envía automáticamente a OpenAI.",
            "Thao tác cần được duyệt. HTTP và tìm kiếm web liên hệ trang đã chọn hoặc DuckDuckGo. Ảnh chụp lưu cục bộ; không thể tự che bí mật trong ảnh. Đầu ra kỹ năng không tự gửi đến OpenAI."));
        entries.Add("Settings.PrivacyControl", new("Your control", "Il tuo controllo", "Votre contrôle", "Deine Kontrolle", "Tu control", "Bạn kiểm soát"));
        entries.Add("Settings.PrivacyControlInfo", new(
            "Viewing these pages makes no AI request. Saving can test the connection if that option is selected. There is no background learner or alarm. Clearing learning data does not erase earlier requests or provider records.",
            "Visitare queste pagine non invia richieste AI. Salvare può verificare la connessione se l’opzione è selezionata. Nessun apprendimento o allarme in background. Cancellare i dati di apprendimento non elimina richieste precedenti o dati del provider.",
            "Consulter ces pages ne lance aucune requête IA. Enregistrer peut tester la connexion si l’option est choisie. Aucun apprentissage ni alarme en arrière-plan. Effacer les données d’apprentissage ne supprime pas les anciennes requêtes ou données du fournisseur.",
            "Diese Seiten anzusehen sendet keine KI-Anfrage. Speichern kann die Verbindung bei gewählter Option testen. Kein Lernen oder Alarm im Hintergrund. Lerndaten löschen entfernt keine früheren Anfragen oder Anbieteraufzeichnungen.",
            "Ver estas páginas no hace solicitudes IA. Guardar puede probar la conexión si se selecciona esa opción. No hay aprendizaje ni alarmas en segundo plano. Borrar aprendizaje no elimina solicitudes previas ni registros del proveedor.",
            "Xem các trang này không gửi yêu cầu AI. Lưu có thể kiểm tra kết nối nếu đã chọn. Không có học hay báo thức nền. Xóa dữ liệu học không xóa yêu cầu cũ hoặc hồ sơ phía nhà cung cấp."));
    }
}
