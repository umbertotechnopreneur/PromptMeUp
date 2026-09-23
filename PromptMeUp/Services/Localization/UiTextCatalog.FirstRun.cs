// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds complete first-run instructions and explicit consent copy in all six supported languages.</summary>
    private static void AddFirstRunEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Oobe.Privacy", new("Privacy", "Privacy", "Confidentialité", "Datenschutz", "Privacidad", "Quyền riêng tư"));
        entries.Add("Oobe.HistoryNotice", new(
            English: "This choice controls learning collection. The separate local request and activity history remains enabled.",
            Italian: "Questa scelta controlla la raccolta per l’apprendimento. La cronologia locale delle richieste e delle attività resta attiva e separata.",
            French: "Ce choix contrôle la collecte pour l’apprentissage. L’historique local distinct des requêtes et activités reste actif.",
            German: "Diese Auswahl steuert die Erfassung zum Lernen. Der separate lokale Anfrage- und Aktivitätsverlauf bleibt aktiv.",
            Spanish: "Esta elección controla la recopilación para aprender. El historial local de solicitudes y actividades sigue activo por separado.",
            Vietnamese: "Lựa chọn này kiểm soát thu thập dữ liệu để học. Lịch sử yêu cầu và hoạt động cục bộ riêng biệt vẫn được bật."));
        entries.Add("Oobe.Terms", new("Terms", "Termini", "Conditions", "Bedingungen", "Condiciones", "Điều khoản"));
        entries.Add("Oobe.QuotaExceeded", new(
            English: "OpenAI reports insufficient API credit or quota. Check billing and usage limits before trying again.",
            Italian: "OpenAI segnala credito o quota API insufficienti. Controlla fatturazione e limiti prima di riprovare.",
            French: "OpenAI signale un crédit ou quota API insuffisant. Vérifiez la facturation et les limites avant de réessayer.",
            German: "OpenAI meldet unzureichendes API-Guthaben oder Kontingent. Prüfe Abrechnung und Limits vor dem nächsten Versuch.",
            Spanish: "OpenAI indica saldo o cuota API insuficientes. Revisa facturación y límites antes de reintentar.",
            Vietnamese: "OpenAI báo số dư hoặc hạn mức API không đủ. Kiểm tra thanh toán và hạn mức trước khi thử lại."));
        entries.Add("Oobe.Welcome", new(
            English: "Welcome to PromptMeUp",
            Italian: "Benvenuto in PromptMeUp",
            French: "Bienvenue dans PromptMeUp",
            German: "Willkommen bei PromptMeUp",
            Spanish: "Te damos la bienvenida a PromptMeUp",
            Vietnamese: "Chào mừng bạn đến với PromptMeUp"));
        entries.Add("Oobe.Benefit", new(
            English: "A little help in your terminal. Ask questions, understand answers, and find the commands you need.",
            Italian: "Un aiuto nel terminale, quando ti serve. Fai domande, chiarisci dubbi e trova i comandi giusti.",
            French: "Un coup de main dans votre terminal. Posez vos questions, comprenez les réponses et trouvez les commandes utiles.",
            German: "Hilfe direkt im Terminal. Stelle Fragen, verstehe Antworten und finde passende Befehle.",
            Spanish: "Ayuda en tu terminal. Haz preguntas, entiende las respuestas y encuentra los comandos que necesitas.",
            Vietnamese: "Trợ giúp ngay trong terminal. Đặt câu hỏi, hiểu câu trả lời và tìm lệnh bạn cần."));
        entries.Add("Oobe.Journey", new(
            English: "Four short steps, then you are ready to go.",
            Italian: "Quattro piccoli passaggi, poi puoi cominciare.",
            French: "Quatre étapes courtes, puis vous pourrez commencer.",
            German: "Vier kurze Schritte, dann kann es losgehen.",
            Spanish: "Cuatro pasos breves y podrás empezar.",
            Vietnamese: "Bốn bước ngắn, rồi bạn có thể bắt đầu."));
        entries.Add("Oobe.InteractiveRequired", new(
            English: "Open a terminal and run hm to complete the welcome. Use hm --help for help.",
            Italian: "Apri un terminale ed esegui hm per completare il benvenuto. Usa hm --help per assistenza.",
            French: "Ouvrez un terminal et lancez hm pour terminer l’accueil. Aide : hm --help.",
            German: "Öffne ein Terminal und starte hm, um die Einrichtung abzuschließen. Hilfe: hm --help.",
            Spanish: "Abre un terminal y ejecuta hm para completar la bienvenida. Ayuda: hm --help.",
            Vietnamese: "Mở terminal và chạy hm để hoàn tất thiết lập. Trợ giúp: hm --help."));
        entries.Add("Oobe.Step", new(
            English: "Step {0} of 4",
            Italian: "Passaggio {0} di 4",
            French: "Étape {0} sur 4",
            German: "Schritt {0} von 4",
            Spanish: "Paso {0} de 4",
            Vietnamese: "Bước {0} trên 4"));
        entries.Add("Oobe.Language", new(
            English: "Let's speak your language",
            Italian: "Parliamo la tua lingua",
            French: "Parlons votre langue",
            German: "Sprechen wir deine Sprache",
            Spanish: "Hablemos tu idioma",
            Vietnamese: "Chọn ngôn ngữ của bạn"));
        entries.Add("Oobe.Start", new(
            English: "First, make yourself at home.",
            Italian: "Prima di tutto, mettiti a tuo agio.",
            French: "Commençons par vous mettre à l’aise.",
            German: "Mach es dir zuerst bequem.",
            Spanish: "Primero, ponte a gusto.",
            Vietnamese: "Trước tiên, hãy chọn điều phù hợp với bạn."));
        entries.Add("Oobe.Detected", new(
            English: "Suggested language: {0}. On the first run, this comes from your computer's language.",
            Italian: "Lingua proposta: {0}. Al primo avvio viene scelta dalla lingua del computer.",
            French: "Langue proposée : {0}. Au premier lancement, elle suit la langue de votre ordinateur.",
            German: "Vorgeschlagene Sprache: {0}. Beim ersten Start entspricht sie der Computersprache.",
            Spanish: "Idioma propuesto: {0}. Al iniciar por primera vez se usa el idioma del equipo.",
            Vietnamese: "Ngôn ngữ đề xuất: {0}. Lần đầu mở ứng dụng sẽ dùng ngôn ngữ của máy tính."));
        entries.Add("Oobe.KeepLanguage", new(
            English: "Continue in {0}",
            Italian: "Continua in {0}",
            French: "Continuer en {0}",
            German: "Mit {0} fortfahren",
            Spanish: "Continuar en {0}",
            Vietnamese: "Tiếp tục bằng {0}"));
        entries.Add("Oobe.ChangeLanguage", new(
            English: "Choose another language",
            Italian: "Cambia lingua",
            French: "Changer de langue",
            German: "Sprache ändern",
            Spanish: "Cambiar idioma",
            Vietnamese: "Đổi ngôn ngữ"));
        entries.Add("Oobe.Exit", new(
            English: "Exit for now",
            Italian: "Esci per ora",
            French: "Quitter pour le moment",
            German: "Vorerst beenden",
            Spanish: "Salir por ahora",
            Vietnamese: "Thoát lúc này"));
        entries.Add("Oobe.Back", new(
            English: "Back",
            Italian: "Indietro",
            French: "Retour",
            German: "Zurück",
            Spanish: "Atrás",
            Vietnamese: "Quay lại"));
        entries.Add("Oobe.Continue", new(
            English: "Continue",
            Italian: "Continua",
            French: "Continuer",
            German: "Weiter",
            Spanish: "Continuar",
            Vietnamese: "Tiếp tục"));
        entries.Add("Oobe.Connect", new(
            English: "Connect OpenAI",
            Italian: "Colleghiamo OpenAI",
            French: "Connectons OpenAI",
            German: "OpenAI verbinden",
            Spanish: "Conectemos OpenAI",
            Vietnamese: "Kết nối OpenAI"));
        entries.Add("Oobe.KeyHelp", new(
            English: "An API key is a secret code that lets PromptMeUp use your OpenAI account. It is not your ChatGPT password.",
            Italian: "La chiave API è un codice segreto che permette a PromptMeUp di usare il tuo account OpenAI. Non è la password di ChatGPT.",
            French: "Une clé API est un code secret qui permet à PromptMeUp d’utiliser votre compte OpenAI. Ce n’est pas votre mot de passe ChatGPT.",
            German: "Ein API-Schlüssel ist ein geheimer Code, mit dem PromptMeUp dein OpenAI-Konto nutzt. Er ist nicht dein ChatGPT-Passwort.",
            Spanish: "Una clave API es un código secreto que permite a PromptMeUp usar tu cuenta de OpenAI. No es tu contraseña de ChatGPT.",
            Vietnamese: "Khóa API là mã bí mật cho phép PromptMeUp dùng tài khoản OpenAI của bạn. Đây không phải mật khẩu ChatGPT."));
        entries.Add("Oobe.Vault", new(
            English: "Your key appears only as dots. After verification, Windows Credential Manager keeps it for your Windows account. It is sent to OpenAI to authenticate, never to the app's author.",
            Italian: "La chiave appare solo come pallini. Dopo la verifica viene custodita da Gestione credenziali di Windows per il tuo account. Viene inviata a OpenAI per autenticarti, mai all’autore dell’app.",
            French: "La clé apparaît uniquement sous forme de points. Après vérification, le Gestionnaire d’identification Windows la conserve pour votre compte. Elle est envoyée à OpenAI pour l’authentification, jamais à l’auteur de l’application.",
            German: "Der Schlüssel erscheint nur als Punkte. Nach der Prüfung verwahrt ihn die Windows-Anmeldeinformationsverwaltung für dein Konto. Er wird zur Anmeldung an OpenAI gesendet, niemals an den App-Autor.",
            Spanish: "La clave se muestra solo como puntos. Tras verificarla, el Administrador de credenciales de Windows la guarda para tu cuenta. Se envía a OpenAI para autenticarte, nunca al autor de la aplicación.",
            Vietnamese: "Khóa chỉ hiển thị dưới dạng dấu chấm. Sau khi xác minh, Windows Credential Manager lưu khóa cho tài khoản Windows của bạn. Khóa được gửi đến OpenAI để xác thực, không gửi cho tác giả ứng dụng."));
        entries.Add("Oobe.SessionStorage", new(
            English: "Your key appears only as dots. On this source build, an entered key lasts for this process only. Configure your shell or secret manager before the next session.",
            Italian: "La chiave appare solo come pallini. In questa versione compilata dai sorgenti, una chiave inserita vale solo per questo processo. Configura la shell o un gestore di segreti prima della prossima sessione.",
            French: "La clé apparaît uniquement sous forme de points. Dans cette version compilée, une clé saisie ne dure que pour ce processus. Configurez votre shell ou gestionnaire de secrets avant la prochaine session.",
            German: "Der Schlüssel erscheint nur als Punkte. In diesem Quellcode-Build gilt ein eingegebener Schlüssel nur für diesen Prozess. Richte vor der nächsten Sitzung deine Shell oder einen Geheimnisspeicher ein.",
            Spanish: "La clave se muestra solo como puntos. En esta compilación desde el código fuente, la clave introducida dura solo este proceso. Configura tu shell o gestor de secretos antes de la próxima sesión.",
            Vietnamese: "Khóa chỉ hiển thị dưới dạng dấu chấm. Bản biên dịch từ mã nguồn này chỉ giữ khóa nhập vào trong tiến trình hiện tại. Hãy cấu hình shell hoặc trình quản lý bí mật trước phiên tiếp theo."));
        entries.Add("Oobe.Portal", new(
            English: "Open the OpenAI key portal",
            Italian: "Apri il portale delle chiavi OpenAI",
            French: "Ouvrir le portail des clés OpenAI",
            German: "OpenAI-Schlüsselportal öffnen",
            Spanish: "Abrir el portal de claves OpenAI",
            Vietnamese: "Mở trang quản lý khóa OpenAI"));
        entries.Add("Oobe.Guide", new(
            English: "How to create an API key",
            Italian: "Come creare una chiave API",
            French: "Comment créer une clé API",
            German: "So erstellst du einen API-Schlüssel",
            Spanish: "Cómo crear una clave API",
            Vietnamese: "Cách tạo khóa API"));
        entries.Add("Oobe.Example", new(
            English: "Example only: sk-proj-ABCD****************WXYZ\nPaste your complete key, not this example. Leave the field empty to go back.",
            Italian: "Solo un esempio: sk-proj-ABCD****************WXYZ\nIncolla la tua chiave completa, non questo esempio. Lascia vuoto per tornare indietro.",
            French: "Exemple uniquement : sk-proj-ABCD****************WXYZ\nCollez votre clé complète, pas cet exemple. Laissez vide pour revenir.",
            German: "Nur ein Beispiel: sk-proj-ABCD****************WXYZ\nFüge deinen vollständigen Schlüssel ein, nicht dieses Beispiel. Leer lassen, um zurückzugehen.",
            Spanish: "Solo un ejemplo: sk-proj-ABCD****************WXYZ\nPega tu clave completa, no este ejemplo. Deja el campo vacío para volver.",
            Vietnamese: "Chỉ là ví dụ: sk-proj-ABCD****************WXYZ\nDán toàn bộ khóa của bạn, không phải ví dụ này. Để trống để quay lại."));
        entries.Add("Oobe.Cost", new(
            English: "Verification sends a short request to OpenAI and may incur a small API charge. API use has its own usage costs.",
            Italian: "La verifica invia una breve richiesta a OpenAI e può generare un piccolo costo API. L’uso delle API ha costi a consumo.",
            French: "La vérification envoie une courte requête à OpenAI et peut entraîner un petit coût API. L’API est facturée à l’usage.",
            German: "Die Prüfung sendet eine kurze Anfrage an OpenAI und kann geringe API-Kosten verursachen. Die API wird nach Nutzung abgerechnet.",
            Spanish: "La verificación envía una solicitud breve a OpenAI y puede generar un pequeño coste API. La API se cobra por uso.",
            Vietnamese: "Xác minh gửi một yêu cầu ngắn đến OpenAI và có thể phát sinh một khoản phí API nhỏ. API tính phí theo mức sử dụng."));
        entries.Add("Oobe.VerifyExisting", new(
            English: "Verify the existing key and continue",
            Italian: "Verifica la chiave già presente e continua",
            French: "Vérifier la clé existante et continuer",
            German: "Vorhandenen Schlüssel prüfen und weiter",
            Spanish: "Verificar la clave existente y continuar",
            Vietnamese: "Xác minh khóa hiện có và tiếp tục"));
        entries.Add("Oobe.ReplaceKey", new(
            English: "Enter a different key",
            Italian: "Inserisci un’altra chiave",
            French: "Saisir une autre clé",
            German: "Anderen Schlüssel eingeben",
            Spanish: "Introducir otra clave",
            Vietnamese: "Nhập khóa khác"));
        entries.Add("Oobe.EnterKey", new(
            English: "Paste a key, verify and continue",
            Italian: "Incolla la chiave, verifica e continua",
            French: "Coller une clé, vérifier et continuer",
            German: "Schlüssel einfügen, prüfen und weiter",
            Spanish: "Pegar clave, verificar y continuar",
            Vietnamese: "Dán khóa, xác minh và tiếp tục"));
        entries.Add("Oobe.KeyLabel", new(
            English: "API key",
            Italian: "Chiave API",
            French: "Clé API",
            German: "API-Schlüssel",
            Spanish: "Clave API",
            Vietnamese: "Khóa API"));
        entries.Add("Oobe.InvalidKey", new(
            English: "Paste the complete OpenAI key without spaces or asterisks.",
            Italian: "Incolla la chiave OpenAI completa, senza spazi o asterischi.",
            French: "Collez la clé OpenAI complète, sans espaces ni astérisques.",
            German: "Füge den vollständigen OpenAI-Schlüssel ohne Leerzeichen oder Sternchen ein.",
            Spanish: "Pega la clave OpenAI completa, sin espacios ni asteriscos.",
            Vietnamese: "Dán toàn bộ khóa OpenAI, không có khoảng trắng hoặc dấu sao."));
        entries.Add("Oobe.Verifying", new(
            English: "Checking OpenAI… You can cancel with Ctrl+C.",
            Italian: "Verifico OpenAI… Puoi annullare con Ctrl+C.",
            French: "Vérification d’OpenAI… Ctrl+C pour annuler.",
            German: "OpenAI wird geprüft… Abbrechen mit Strg+C.",
            Spanish: "Verificando OpenAI… Puedes cancelar con Ctrl+C.",
            Vietnamese: "Đang kiểm tra OpenAI… Nhấn Ctrl+C để hủy."));
        entries.Add("Oobe.Connected", new(
            English: "Connected! Two more steps and you are ready.",
            Italian: "Connessione riuscita! Ancora due passaggi e ci siamo.",
            French: "Connexion réussie ! Encore deux étapes et tout sera prêt.",
            German: "Verbunden! Noch zwei Schritte, dann bist du bereit.",
            Spanish: "¡Conexión correcta! Dos pasos más y estará listo.",
            Vietnamese: "Kết nối thành công! Chỉ còn hai bước nữa."));
        entries.Add("Oobe.Retry", new(
            English: "Try again",
            Italian: "Riprova",
            French: "Réessayer",
            German: "Erneut versuchen",
            Spanish: "Reintentar",
            Vietnamese: "Thử lại"));
        entries.Add("Oobe.Billing", new(
            English: "Open OpenAI billing",
            Italian: "Apri la fatturazione OpenAI",
            French: "Ouvrir la facturation OpenAI",
            German: "OpenAI-Abrechnung öffnen",
            Spanish: "Abrir facturación OpenAI",
            Vietnamese: "Mở trang thanh toán OpenAI"));
        entries.Add("Oobe.KeyRejected", new(
            English: "OpenAI did not accept this key. Check that you copied it completely and that it has not been revoked.",
            Italian: "OpenAI non ha accettato la chiave. Controlla di averla copiata tutta e che non sia stata revocata.",
            French: "OpenAI a refusé cette clé. Vérifiez qu’elle est complète et n’a pas été révoquée.",
            German: "OpenAI hat den Schlüssel abgelehnt. Prüfe, ob er vollständig kopiert und nicht widerrufen wurde.",
            Spanish: "OpenAI no aceptó la clave. Comprueba que esté completa y que no se haya revocado.",
            Vietnamese: "OpenAI không chấp nhận khóa. Kiểm tra xem bạn đã sao chép đầy đủ và khóa chưa bị thu hồi."));
        entries.Add("Oobe.AccessDenied", new(
            English: "This account cannot use the selected model. Check the key's project permissions and model access in OpenAI.",
            Italian: "Questo account non può usare il modello selezionato. Controlla su OpenAI i permessi della chiave e l’accesso al modello.",
            French: "Ce compte ne peut pas utiliser le modèle choisi. Vérifiez les autorisations de la clé et l’accès au modèle dans OpenAI.",
            German: "Dieses Konto kann das gewählte Modell nicht nutzen. Prüfe Schlüsselberechtigungen und Modellzugriff bei OpenAI.",
            Spanish: "Esta cuenta no puede usar el modelo seleccionado. Revisa los permisos de la clave y el acceso al modelo en OpenAI.",
            Vietnamese: "Tài khoản không thể dùng mô hình đã chọn. Kiểm tra quyền của khóa và quyền truy cập mô hình trên OpenAI."));
        entries.Add("Oobe.RateLimited", new(
            English: "OpenAI reported a usage limit. Check your API credit and limits, or wait a little before retrying.",
            Italian: "OpenAI segnala un limite di utilizzo. Controlla credito e limiti API, oppure attendi un po’ prima di riprovare.",
            French: "OpenAI signale une limite d’utilisation. Vérifiez votre crédit et vos limites API, ou attendez avant de réessayer.",
            German: "OpenAI meldet ein Nutzungslimit. Prüfe API-Guthaben und Limits oder warte vor dem nächsten Versuch.",
            Spanish: "OpenAI indica un límite de uso. Revisa el saldo y los límites API, o espera antes de reintentar.",
            Vietnamese: "OpenAI báo giới hạn sử dụng. Kiểm tra số dư và hạn mức API, hoặc chờ trước khi thử lại."));
        entries.Add("Oobe.NetworkError", new(
            English: "OpenAI could not be reached. Check your connection and try again; you do not need to paste the key again.",
            Italian: "Non riesco a raggiungere OpenAI. Controlla la connessione e riprova: non serve incollare di nuovo la chiave.",
            French: "Impossible de joindre OpenAI. Vérifiez la connexion et réessayez ; inutile de recoller la clé.",
            German: "OpenAI ist nicht erreichbar. Prüfe die Verbindung und versuche es erneut; du musst den Schlüssel nicht neu einfügen.",
            Spanish: "No se pudo contactar con OpenAI. Revisa la conexión y reintenta; no necesitas volver a pegar la clave.",
            Vietnamese: "Không thể kết nối OpenAI. Kiểm tra mạng và thử lại; bạn không cần dán lại khóa."));
        entries.Add("Oobe.ServiceError", new(
            English: "OpenAI could not complete the check. Try again shortly. Your welcome is not marked complete.",
            Italian: "OpenAI non ha completato la verifica. Riprova tra poco. Il primo avvio non è stato segnato come completato.",
            French: "OpenAI n’a pas pu terminer la vérification. Réessayez bientôt. L’accueil n’est pas marqué comme terminé.",
            German: "OpenAI konnte die Prüfung nicht abschließen. Versuche es später erneut. Die Einrichtung bleibt unvollständig.",
            Spanish: "OpenAI no pudo completar la verificación. Reintenta en breve. La bienvenida aún no se considera completada.",
            Vietnamese: "OpenAI chưa hoàn tất kiểm tra. Hãy thử lại sau. Thiết lập ban đầu chưa được đánh dấu hoàn tất."));
        entries.Add("Oobe.StorageError", new(
            English: "The operating system could not save the key. Try again or exit; no unprotected copy will be saved.",
            Italian: "Il sistema operativo non riesce a custodire la chiave. Riprova oppure esci: non verrà salvata una copia non protetta.",
            French: "Le système ne peut pas enregistrer la clé. Réessayez ou quittez ; aucune copie non protégée ne sera enregistrée.",
            German: "Das Betriebssystem konnte den Schlüssel nicht speichern. Wiederholen oder beenden; es wird keine ungeschützte Kopie gespeichert.",
            Spanish: "El sistema no pudo guardar la clave. Reintenta o sal; no se guardará una copia sin protección.",
            Vietnamese: "Hệ điều hành không thể lưu khóa. Thử lại hoặc thoát; không lưu bản sao không được bảo vệ."));
        entries.Add("Oobe.Name", new(
            English: "What should I call you?",
            Italian: "Come vuoi essere chiamato?",
            French: "Comment vous appeler ?",
            German: "Wie darf ich dich nennen?",
            Spanish: "¿Cómo quieres que te llame?",
            Vietnamese: "Bạn muốn được gọi là gì?"));
        entries.Add("Oobe.TwoLeft", new(
            English: "The connection is ready. Let's make this yours.",
            Italian: "La connessione è pronta. Ora rendiamolo un po’ tuo.",
            French: "La connexion est prête. Personnalisons la suite.",
            German: "Die Verbindung steht. Jetzt wird es persönlich.",
            Spanish: "La conexión está lista. Vamos a personalizarlo.",
            Vietnamese: "Kết nối đã sẵn sàng. Giờ hãy cá nhân hóa một chút."));
        entries.Add("Oobe.NameHelp", new(
            English: "A name or nickname is enough. This is optional: leave it blank to skip.",
            Italian: "Basta un nome o un soprannome. È facoltativo: lascia vuoto per saltare.",
            French: "Un prénom ou un surnom suffit. C’est facultatif : laissez vide pour passer.",
            German: "Ein Name oder Spitzname genügt. Freiwillig: zum Überspringen leer lassen.",
            Spanish: "Basta un nombre o apodo. Es opcional: déjalo vacío para omitirlo.",
            Vietnamese: "Chỉ cần tên hoặc biệt danh. Không bắt buộc: để trống để bỏ qua."));
        entries.Add("Oobe.NameLabel", new(
            English: "Name or nickname",
            Italian: "Nome o soprannome",
            French: "Nom ou surnom",
            German: "Name oder Spitzname",
            Spanish: "Nombre o apodo",
            Vietnamese: "Tên hoặc biệt danh"));
        entries.Add("Oobe.Memory", new(
            English: "Choose what can be remembered",
            Italian: "Scegli cosa può ricordare",
            French: "Choisissez ce qui peut être retenu",
            German: "Wähle, was gespeichert werden darf",
            Spanish: "Elige qué puede recordar",
            Vietnamese: "Chọn điều có thể được ghi nhớ"));
        entries.Add("Oobe.AlmostThere", new(
            English: "Last step. Nearly there!",
            Italian: "Ultimo passaggio. Ci siamo quasi!",
            French: "Dernière étape. On y est presque !",
            German: "Letzter Schritt. Fast geschafft!",
            Spanish: "Último paso. ¡Ya casi está!",
            Vietnamese: "Bước cuối. Sắp xong rồi!"));
        entries.Add("Oobe.MemoryHelp", new(
            English: "Memories keep useful preferences and details for future conversations. Enabling them does not start recording chats or activate individual skills.",
            Italian: "Le memorie conservano preferenze e dettagli utili alle conversazioni future. Abilitarle non avvia la raccolta delle chat e non attiva le singole skills.",
            French: "Les mémoires conservent des préférences et détails utiles aux échanges futurs. Les activer ne démarre pas la collecte des chats ni les skills individuels.",
            German: "Erinnerungen speichern nützliche Vorlieben und Details für spätere Gespräche. Ihre Aktivierung startet weder die Chat-Erfassung noch einzelne Skills.",
            Spanish: "Las memorias guardan preferencias y detalles útiles para futuras conversaciones. Activarlas no inicia la recopilación de chats ni activa skills individuales.",
            Vietnamese: "Bộ nhớ lưu sở thích và chi tiết hữu ích cho các cuộc trò chuyện sau. Bật bộ nhớ không tự thu thập chat hay bật từng skill."));
        entries.Add("Oobe.EnableMemory", new(
            English: "Enable memories?",
            Italian: "Abilitare le memorie?",
            French: "Activer les mémoires ?",
            German: "Erinnerungen aktivieren?",
            Spanish: "¿Activar las memorias?",
            Vietnamese: "Bật bộ nhớ?"));
        entries.Add("Oobe.DreamHelp", new(
            English: "Dreaming means revisiting past chats to learn and propose useful details to remember. You review the proposals before they become memories.",
            Italian: "Far “sognare” l’AI significa rileggere le chat passate per apprendere e proporre dettagli utili da ricordare. Rivedi tu le proposte prima che diventino memorie.",
            French: "Faire « rêver » l’IA signifie relire les chats passés pour apprendre et proposer des détails utiles à retenir. Vous examinez les propositions avant leur mémorisation.",
            German: "Beim „Träumen“ liest die KI frühere Chats, um zu lernen und nützliche Erinnerungen vorzuschlagen. Du prüfst die Vorschläge, bevor sie gespeichert werden.",
            Spanish: "Hacer «soñar» a la IA significa releer chats anteriores para aprender y proponer detalles útiles. Tú revisas las propuestas antes de guardarlas como memorias.",
            Vietnamese: "Cho AI “mơ” nghĩa là đọc lại các chat cũ để học và đề xuất chi tiết hữu ích cần nhớ. Bạn duyệt đề xuất trước khi chúng thành bộ nhớ."));
        entries.Add("Oobe.RecordingHelp", new(
            English: "Chat collection for learning is off by default. Opting in stores redacted chat excerpts locally, with recognizable secrets obscured. During dreaming, relevant excerpts are sent to OpenAI. You can manage this with hm --learning.",
            Italian: "La raccolta delle chat per l’apprendimento è disattivata di default. Se la abiliti, gli estratti delle chat vengono salvati localmente, con i segreti riconoscibili oscurati. Durante il sogno, gli estratti necessari vengono inviati a OpenAI. Gestisci questa funzione con hm --learning.",
            French: "La collecte pour l’apprentissage est désactivée par défaut. Si vous l’activez, des extraits de chats sont conservés localement avec les secrets reconnaissables masqués. Pendant le rêve, les extraits utiles sont envoyés à OpenAI. Gestion : hm --learning.",
            German: "Die Chat-Erfassung zum Lernen ist standardmäßig aus. Bei Zustimmung werden Chatauszüge lokal mit unkenntlich gemachten erkennbaren Geheimnissen gespeichert. Beim Träumen werden relevante Auszüge an OpenAI gesendet. Verwaltung: hm --learning.",
            Spanish: "La recopilación para aprender está desactivada por defecto. Al activarla se guardan extractos locales con los secretos reconocibles ocultos. Al soñar, los extractos necesarios se envían a OpenAI. Se gestiona con hm --learning.",
            Vietnamese: "Thu thập chat để học mặc định tắt. Nếu bật, các đoạn chat được lưu cục bộ với bí mật nhận diện được đã che đi. Khi mơ, các đoạn cần thiết được gửi đến OpenAI. Quản lý bằng hm --learning."));
        entries.Add("Oobe.EnableRecording", new(
            English: "Save redacted chat excerpts locally for learning?",
            Italian: "Salvare localmente gli estratti redatti delle chat per l’apprendimento?",
            French: "Conserver localement des extraits expurgés pour l’apprentissage ?",
            German: "Bereinigte Chatauszüge lokal zum Lernen speichern?",
            Spanish: "¿Guardar extractos depurados localmente para aprender?",
            Vietnamese: "Lưu cục bộ các đoạn chat đã che thông tin để học?"));
        entries.Add("Oobe.RecordingOff", new(
            English: "Chat collection for learning stays off.",
            Italian: "La raccolta delle chat per l’apprendimento resta disattivata.",
            French: "La collecte pour l’apprentissage reste désactivée.",
            German: "Die Chat-Erfassung zum Lernen bleibt aus.",
            Spanish: "La recopilación para aprender sigue desactivada.",
            Vietnamese: "Thu thập chat để học vẫn tắt."));
        entries.Add("Oobe.ClearLearning", new(
            English: "Turning this off clears existing learning material and pending proposals.",
            Italian: "Disattivando questa funzione elimini il materiale di apprendimento e le proposte in attesa già presenti.",
            French: "La désactivation supprime les données d’apprentissage et les propositions en attente.",
            German: "Das Deaktivieren löscht vorhandenes Lernmaterial und offene Vorschläge.",
            Spanish: "Al desactivarlo se eliminan los datos de aprendizaje y propuestas pendientes.",
            Vietnamese: "Tắt tính năng sẽ xóa dữ liệu học và các đề xuất đang chờ."));
        entries.Add("Oobe.ConfirmClear", new(
            English: "Continue and clear that learning material?",
            Italian: "Continuare ed eliminare quel materiale di apprendimento?",
            French: "Continuer et supprimer ces données d’apprentissage ?",
            German: "Fortfahren und dieses Lernmaterial löschen?",
            Spanish: "¿Continuar y borrar esos datos de aprendizaje?",
            Vietnamese: "Tiếp tục và xóa dữ liệu học đó?"));
        entries.Add("Oobe.DirectNotice", new(
            English: "By default, eligible commands can run after a cancellable five-second countdown and safety checks. You can require confirmation instead.",
            Italian: "Di default, i comandi idonei possono partire dopo i controlli di sicurezza e un conto alla rovescia annullabile di cinque secondi. Puoi invece richiedere la conferma.",
            French: "Par défaut, les commandes admissibles peuvent démarrer après les contrôles et un compte à rebours annulable de cinq secondes. Vous pouvez exiger une confirmation.",
            German: "Standardmäßig können geeignete Befehle nach Sicherheitsprüfungen und einem abbrechbaren Fünf-Sekunden-Countdown starten. Du kannst stattdessen eine Bestätigung verlangen.",
            Spanish: "Por defecto, los comandos aptos pueden ejecutarse tras los controles y una cuenta atrás cancelable de cinco segundos. Puedes exigir confirmación.",
            Vietnamese: "Mặc định, lệnh đủ điều kiện có thể chạy sau kiểm tra an toàn và đếm ngược năm giây có thể hủy. Bạn có thể yêu cầu xác nhận thay thế."));
        entries.Add("Oobe.ConfirmCommands", new(
            English: "Require confirmation before running commands?",
            Italian: "Richiedere conferma prima di eseguire i comandi?",
            French: "Exiger une confirmation avant les commandes ?",
            German: "Vor der Befehlsausführung eine Bestätigung verlangen?",
            Spanish: "¿Exigir confirmación antes de ejecutar comandos?",
            Vietnamese: "Yêu cầu xác nhận trước khi chạy lệnh?"));
        entries.Add("Oobe.Ready", new(
            English: "We did it! Everything is ready.",
            Italian: "Ce l’abbiamo fatta! Tutto pronto.",
            French: "C’est fait ! Tout est prêt.",
            German: "Geschafft! Alles ist bereit.",
            Spanish: "¡Lo conseguimos! Todo listo.",
            Vietnamese: "Xong rồi! Mọi thứ đã sẵn sàng."));
        entries.Add("Oobe.Thanks", new(
            English: "Thanks for setting up PromptMeUp.",
            Italian: "Grazie per aver configurato PromptMeUp.",
            French: "Merci d’avoir configuré PromptMeUp.",
            German: "Danke, dass du PromptMeUp eingerichtet hast.",
            Spanish: "Gracias por configurar PromptMeUp.",
            Vietnamese: "Cảm ơn bạn đã thiết lập PromptMeUp."));
        entries.Add("Oobe.ThanksName", new(
            English: "Thanks, {0}, for setting up PromptMeUp.",
            Italian: "Grazie, {0}, per aver configurato PromptMeUp.",
            French: "Merci, {0}, d’avoir configuré PromptMeUp.",
            German: "Danke, {0}, dass du PromptMeUp eingerichtet hast.",
            Spanish: "Gracias, {0}, por configurar PromptMeUp.",
            Vietnamese: "Cảm ơn {0} đã thiết lập PromptMeUp."));
        entries.Add("Oobe.StartUsing", new(
            English: "Use hm from any folder in your terminal once its command alias or PATH is configured. For help: hm --help.",
            Italian: "Puoi usare hm da qualsiasi cartella del terminale quando l’alias o il PATH è configurato. Per assistenza: hm --help.",
            French: "Utilisez hm depuis tout dossier du terminal une fois son alias ou le PATH configuré. Aide : hm --help.",
            German: "Nutze hm nach Einrichtung des Befehlsalias oder PATH aus jedem Terminalordner. Hilfe: hm --help.",
            Spanish: "Usa hm desde cualquier carpeta del terminal cuando el alias o PATH esté configurado. Ayuda: hm --help.",
            Vietnamese: "Dùng hm từ mọi thư mục trong terminal sau khi cấu hình bí danh lệnh hoặc PATH. Trợ giúp: hm --help."));
        entries.Add("Oobe.Skills", new(
            English: "Skills are reusable instructions for specific tasks. They are available but disabled by default. Explore them with hm --skills.",
            Italian: "Le skills sono istruzioni riutilizzabili per attività specifiche. Sono disponibili ma disattivate di default. Scoprile con hm --skills.",
            French: "Les skills sont des instructions réutilisables pour des tâches précises. Ils sont disponibles mais désactivés par défaut. Découvrez-les avec hm --skills.",
            German: "Skills sind wiederverwendbare Anweisungen für bestimmte Aufgaben. Sie sind verfügbar, aber standardmäßig deaktiviert. Entdecke sie mit hm --skills.",
            Spanish: "Las skills son instrucciones reutilizables para tareas concretas. Están disponibles pero desactivadas por defecto. Descúbrelas con hm --skills.",
            Vietnamese: "Skills là hướng dẫn dùng lại cho tác vụ cụ thể. Có sẵn nhưng mặc định tắt. Khám phá bằng hm --skills."));
        entries.Add("Oobe.ChangeLater", new(
            English: "You can change your preferences at any time with hm --setup.",
            Italian: "Puoi cambiare le tue preferenze quando vuoi con hm --setup.",
            French: "Modifiez vos préférences à tout moment avec hm --setup.",
            German: "Ändere deine Einstellungen jederzeit mit hm --setup.",
            Spanish: "Cambia tus preferencias cuando quieras con hm --setup.",
            Vietnamese: "Bạn có thể đổi tùy chọn bất cứ lúc nào bằng hm --setup."));
    }
}
