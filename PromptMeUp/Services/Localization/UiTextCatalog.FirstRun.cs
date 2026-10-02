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
            English: "Three short steps, then we can get started.",
            Italian: "Tre passaggi insieme, poi possiamo cominciare.",
            French: "Trois étapes courtes, puis nous pourrons commencer.",
            German: "Drei kurze Schritte, dann können wir loslegen.",
            Spanish: "Tres pasos breves y podremos empezar.",
            Vietnamese: "Ba bước ngắn, rồi chúng ta có thể bắt đầu."));
        entries.Add("Oobe.InteractiveRequired", new(
            English: "Open a terminal and run hm to complete the welcome. Use hm --help for help.",
            Italian: "Apri un terminale ed esegui hm per completare il benvenuto. Usa hm --help per assistenza.",
            French: "Ouvrez un terminal et lancez hm pour terminer l’accueil. Aide : hm --help.",
            German: "Öffne ein Terminal und starte hm, um die Einrichtung abzuschließen. Hilfe: hm --help.",
            Spanish: "Abre un terminal y ejecuta hm para completar la bienvenida. Ayuda: hm --help.",
            Vietnamese: "Mở terminal và chạy hm để hoàn tất thiết lập. Trợ giúp: hm --help."));
        entries.Add("Oobe.Step", new(
            English: "Step {0} of 3",
            Italian: "Passaggio {0} di 3",
            French: "Étape {0} sur 3",
            German: "Schritt {0} von 3",
            Spanish: "Paso {0} de 3",
            Vietnamese: "Bước {0} trên 3"));
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
        entries.Add("Oobe.Connect", new(
            English: "Connect OpenAI",
            Italian: "Colleghiamo OpenAI",
            French: "Connectons OpenAI",
            German: "OpenAI verbinden",
            Spanish: "Conectemos OpenAI",
            Vietnamese: "Kết nối OpenAI"));
        entries.Add("Oobe.KeyHelp", new(
            English: "To talk with me, connect your OpenAI API key. This is separate from your ChatGPT password.",
            Italian: "Per parlare con me, collega la tua chiave API OpenAI. È diversa dalla password di ChatGPT.",
            French: "Pour discuter avec moi, connectez votre clé API OpenAI. Elle est distincte de votre mot de passe ChatGPT.",
            German: "Um mit mir zu sprechen, verbinde deinen OpenAI-API-Schlüssel. Das ist nicht dein ChatGPT-Passwort.",
            Spanish: "Para hablar conmigo, conecta tu clave API de OpenAI. No es tu contraseña de ChatGPT.",
            Vietnamese: "Để trò chuyện với tôi, hãy kết nối khóa API OpenAI. Khóa này khác mật khẩu ChatGPT."));
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
        entries.Add("Oobe.Personalize", new(
            English: "Let's make this yours",
            Italian: "Facciamolo tuo",
            French: "Personnalisons votre expérience",
            German: "Machen wir es zu deinem Begleiter",
            Spanish: "Hagámoslo tuyo",
            Vietnamese: "Cá nhân hóa theo cách của bạn"));
        entries.Add("Oobe.TwoLeft", new(
            English: "The connection is ready. Tell me how you'd like to work together.",
            Italian: "La connessione è pronta. Dimmi come vuoi che lavoriamo insieme.",
            French: "La connexion est prête. Dites-moi comment vous souhaitez travailler avec moi.",
            German: "Die Verbindung steht. Sag mir, wie wir zusammenarbeiten sollen.",
            Spanish: "La conexión está lista. Dime cómo quieres que trabajemos juntos.",
            Vietnamese: "Kết nối đã sẵn sàng. Hãy cho tôi biết bạn muốn chúng ta làm việc cùng nhau thế nào."));
        entries.Add("Oobe.NameHelp", new(
            English: "A first name or nickname is enough. Press Enter to move on; leave it blank if you prefer.",
            Italian: "Mi basta un nome o un soprannome. Premi Invio per continuare; se preferisci, lascia vuoto.",
            French: "Un prénom ou un surnom suffit. Appuyez sur Entrée pour continuer ou laissez vide.",
            German: "Ein Vorname oder Spitzname reicht. Drücke Enter, um fortzufahren; leer lassen geht auch.",
            Spanish: "Basta un nombre o apodo. Pulsa Intro para continuar o déjalo vacío.",
            Vietnamese: "Chỉ cần tên hoặc biệt danh. Nhấn Enter để tiếp tục hoặc để trống nếu muốn."));
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
        entries.Add("Oobe.SkillTitle", new(
            English: "Skills for your kind of work",
            Italian: "Skill per quello che fai",
            French: "Des skills pour vos tâches",
            German: "Skills für deine Aufgaben",
            Spanish: "Skills para tus tareas",
            Vietnamese: "Skills cho công việc của bạn"));
        entries.Add("Oobe.SkillHelp", new(
            English: "A skill is reusable guidance for a specific task. For matching requests, its instructions go to OpenAI. Choose included skills here; review imported ones with hm --skills.",
            Italian: "Una skill è una guida riutilizzabile per un compito preciso. Per richieste pertinenti, le sue istruzioni vanno a OpenAI. Scegli qui le skill incluse; rivedi quelle importate con hm --skills.",
            French: "Une skill guide une tâche précise. Pour les demandes pertinentes, ses instructions vont à OpenAI. Choisissez les skills incluses ici ; vérifiez les imports avec hm --skills.",
            German: "Ein Skill ist eine Anleitung für eine Aufgabe. Bei passenden Anfragen gehen seine Anweisungen an OpenAI. Wähle hier mitgelieferte Skills; Importe prüfst du mit hm --skills.",
            Spanish: "Una skill es una guía para una tarea concreta. En solicitudes pertinentes, sus instrucciones van a OpenAI. Elige aquí las skills incluidas; revisa las importadas con hm --skills.",
            Vietnamese: "Skill là hướng dẫn cho tác vụ cụ thể. Với yêu cầu phù hợp, hướng dẫn được gửi đến OpenAI. Chọn skills có sẵn tại đây; xem skills nhập vào bằng hm --skills."));
        entries.Add("Oobe.EnableSkills", new(
            English: "Would you like to enable some skills now?",
            Italian: "Vuoi attivare qualche skill adesso?",
            French: "Voulez-vous activer des skills maintenant ?",
            German: "Möchtest du jetzt Skills aktivieren?",
            Spanish: "¿Quieres activar algunas skills ahora?",
            Vietnamese: "Bạn muốn bật một số skills ngay bây giờ?"));
        entries.Add("Oobe.SkillsPrompt", new(
            English: "Choose the skills I may use",
            Italian: "Scegli le skill che potrò usare",
            French: "Choisissez les skills que je peux utiliser",
            German: "Wähle die Skills, die ich nutzen darf",
            Spanish: "Elige las skills que puedo usar",
            Vietnamese: "Chọn skills mà tôi được phép dùng"));
        entries.Add("Oobe.SkillsInstructions", new(
            English: "Space selects; Enter confirms. Leave all unchecked to enable none.",
            Italian: "Spazio seleziona; Invio conferma. Lascia tutto vuoto per non attivarne nessuna.",
            French: "Espace sélectionne ; Entrée confirme. Ne cochez rien pour n'en activer aucune.",
            German: "Leertaste wählt aus; Enter bestätigt. Ohne Auswahl bleibt alles aus.",
            Spanish: "Espacio selecciona; Intro confirma. No marques ninguna para dejarlas desactivadas.",
            Vietnamese: "Nhấn Space để chọn; Enter để xác nhận. Không chọn gì nếu không muốn bật skill nào."));
        entries.Add("Oobe.SkillsSelected", new(
            English: "Skills selected: {0}. You can review them with hm --skills.",
            Italian: "Skill selezionate: {0}. Puoi rivederle con hm --skills.",
            French: "Skills sélectionnées : {0}. Vérifiez-les avec hm --skills.",
            German: "Ausgewählte Skills: {0}. Du kannst sie mit hm --skills prüfen.",
            Spanish: "Skills seleccionadas: {0}. Revísalas con hm --skills.",
            Vietnamese: "Skills đã chọn: {0}. Bạn có thể xem lại bằng hm --skills."));
        entries.Add("Oobe.SkillsEmpty", new(
            English: "There are no usable skills to enable yet. You can add or review them later with hm --skills.",
            Italian: "Non ci sono ancora skill utilizzabili. Potrai aggiungerle o rivederle con hm --skills.",
            French: "Aucune skill utilisable pour le moment. Vous pourrez en ajouter ou les revoir avec hm --skills.",
            German: "Noch keine nutzbaren Skills vorhanden. Mit hm --skills kannst du später welche hinzufügen oder prüfen.",
            Spanish: "Aún no hay skills utilizables. Podrás añadirlas o revisarlas con hm --skills.",
            Vietnamese: "Chưa có skills nào dùng được. Bạn có thể thêm hoặc xem lại sau bằng hm --skills."));
        entries.Add("Oobe.CommandMode", new(
            English: "How should I handle commands?",
            Italian: "Come gestiamo i comandi?",
            French: "Comment gérer les commandes ?",
            German: "Wie soll ich Befehle behandeln?",
            Spanish: "¿Cómo gestionamos los comandos?",
            Vietnamese: "Tôi nên xử lý lệnh thế nào?"));
        entries.Add("Oobe.MemoryHelp", new(
            English: "Memories let me keep details you choose to save. This also unlocks skills; you'll choose which ones next. Chat collection stays off unless you opt in.",
            Italian: "I ricordi mi aiutano a tenere i dettagli che scegli di salvare. Questa scelta rende disponibili anche le skill: deciderai quali attivare tra poco. La raccolta delle chat resta spenta finché non la autorizzi.",
            French: "Les souvenirs gardent les détails que vous choisissez d'enregistrer. Ce choix ouvre aussi les skills, à sélectionner ensuite. La collecte des chats reste désactivée sans votre accord.",
            German: "Erinnerungen bewahren Details auf, die du selbst speicherst. Damit werden auch Skills verfügbar; du wählst sie gleich aus. Ohne Zustimmung werden Chats nicht gesammelt.",
            Spanish: "Los recuerdos guardan los detalles que eliges conservar. Esto también permite elegir skills después. La recopilación de chats sigue apagada sin tu permiso.",
            Vietnamese: "Bộ nhớ giữ các chi tiết bạn chọn lưu. Tùy chọn này cũng mở skills để bạn chọn tiếp. Chat không được thu thập nếu bạn không đồng ý."));
        entries.Add("Oobe.EnableMemory", new(
            English: "Enable memories and make skills available?",
            Italian: "Attivare i ricordi e rendere disponibili le skill?",
            French: "Activer les souvenirs et rendre les skills disponibles ?",
            German: "Erinnerungen aktivieren und Skills verfügbar machen?",
            Spanish: "¿Activar los recuerdos y habilitar las skills?",
            Vietnamese: "Bật bộ nhớ và cho phép dùng skills?"));
        entries.Add("Oobe.RecordingHelp", new(
            English: "If you opt in, redacted chat excerpts are saved locally. When you ask me to review them for memories, relevant excerpts go to OpenAI. Manage this with hm --learning.",
            Italian: "Se acconsenti, salvo sul computer estratti delle chat con i segreti riconoscibili oscurati. Solo quando chiedi di rivederli per proporre ricordi, gli estratti utili vanno a OpenAI. Gestisci tutto con hm --learning.",
            French: "Si vous acceptez, des extraits de chat expurgés restent sur votre appareil. Si vous demandez leur analyse pour proposer des souvenirs, les extraits utiles sont envoyés à OpenAI. Gestion : hm --learning.",
            German: "Wenn du zustimmst, speichere ich bereinigte Chatauszüge lokal. Erst wenn du eine Analyse für Erinnerungen anforderst, gehen relevante Auszüge an OpenAI. Verwaltung: hm --learning.",
            Spanish: "Si aceptas, guardo extractos de chat depurados en tu equipo. Cuando pides revisarlos para proponer recuerdos, los fragmentos útiles van a OpenAI. Gestión: hm --learning.",
            Vietnamese: "Nếu đồng ý, các đoạn chat đã che bí mật được lưu cục bộ. Khi bạn yêu cầu xem lại để đề xuất ký ức, những đoạn liên quan sẽ được gửi đến OpenAI. Quản lý bằng hm --learning."));
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
            English: "Eligible commands can run after safety checks and a cancellable five-second countdown. Choose Yes if you'd rather confirm each one.",
            Italian: "I comandi idonei possono partire dopo i controlli di sicurezza e cinque secondi annullabili. Scegli Sì se vuoi confermare ogni comando.",
            French: "Les commandes admissibles peuvent démarrer après les contrôles et cinq secondes annulables. Choisissez Oui pour confirmer chaque commande.",
            German: "Geeignete Befehle können nach Sicherheitsprüfungen und fünf abbrechbaren Sekunden starten. Wähle Ja, um jeden Befehl zu bestätigen.",
            Spanish: "Los comandos aptos pueden iniciarse tras las comprobaciones y cinco segundos cancelables. Elige Sí para confirmar cada comando.",
            Vietnamese: "Lệnh phù hợp có thể chạy sau khi kiểm tra và đếm ngược năm giây có thể hủy. Chọn Có nếu muốn xác nhận từng lệnh."));
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
            English: "You're all set. I'll be here whenever you need a hand.",
            Italian: "Ci siamo. Sono qui ogni volta che ti serve una mano.",
            French: "Tout est prêt. Je suis là quand vous avez besoin d'un coup de main.",
            German: "Alles ist bereit. Ich bin da, wenn du Hilfe brauchst.",
            Spanish: "Todo listo. Estaré aquí cuando necesites una mano.",
            Vietnamese: "Mọi thứ đã sẵn sàng. Tôi luôn ở đây khi bạn cần giúp đỡ."));
        entries.Add("Oobe.ThanksName", new(
            English: "All set, {0}. I'll be here whenever you need a hand.",
            Italian: "Ci siamo, {0}. Sono qui ogni volta che ti serve una mano.",
            French: "Tout est prêt, {0}. Je suis là quand vous avez besoin d'un coup de main.",
            German: "Alles bereit, {0}. Ich bin da, wenn du Hilfe brauchst.",
            Spanish: "Todo listo, {0}. Estaré aquí cuando necesites una mano.",
            Vietnamese: "Xong rồi, {0}. Tôi luôn ở đây khi bạn cần giúp đỡ."));
        entries.Add("Oobe.StartUsing", new(
            English: "Once hm is on your PATH or available as an alias, try one of these. More help: hm --help.",
            Italian: "Quando hm è nel PATH o disponibile come alias, prova una di queste. Per il resto: hm --help.",
            French: "Une fois hm dans le PATH ou disponible comme alias, essayez l'une de ces commandes. Aide : hm --help.",
            German: "Sobald hm im PATH oder als Alias verfügbar ist, probiere einen dieser Befehle. Hilfe: hm --help.",
            Spanish: "Cuando hm esté en el PATH o disponible como alias, prueba una de estas opciones. Ayuda: hm --help.",
            Vietnamese: "Khi hm có trong PATH hoặc dùng được qua bí danh, hãy thử một trong các lệnh này. Trợ giúp: hm --help."));
        entries.Add("Oobe.TryFirst", new(
            English: "Try your first useful task",
            Italian: "Prova subito qualcosa di utile",
            French: "Essayez votre première tâche utile",
            German: "Probiere deine erste nützliche Aufgabe",
            Spanish: "Prueba tu primera tarea útil",
            Vietnamese: "Thử tác vụ hữu ích đầu tiên"));
        entries.Add("Oobe.TryAsk", new(
            English: "Ask for a command or a clear explanation.",
            Italian: "Chiedi un comando o una spiegazione chiara.",
            French: "Demandez une commande ou une explication claire.",
            German: "Frage nach einem Befehl oder einer klaren Erklärung.",
            Spanish: "Pide un comando o una explicación clara.",
            Vietnamese: "Yêu cầu một lệnh hoặc lời giải thích rõ ràng."));
        entries.Add("Oobe.TryAskCommand", new(
            English: "How do I list large files?",
            Italian: "Come trovo i file grandi?",
            French: "Comment trouver les gros fichiers ?",
            German: "Wie finde ich große Dateien?",
            Spanish: "¿Cómo encuentro archivos grandes?",
            Vietnamese: "Làm sao tìm tệp lớn?"));
        entries.Add("Oobe.TryChat", new(
            English: "Open a conversation and ask follow-up questions.",
            Italian: "Apri una conversazione e fai domande successive.",
            French: "Ouvrez une conversation et posez des questions de suivi.",
            German: "Starte ein Gespräch und stelle Folgefragen.",
            Spanish: "Abre una conversación y haz preguntas de seguimiento.",
            Vietnamese: "Mở cuộc trò chuyện và đặt câu hỏi tiếp theo."));
        entries.Add("Oobe.TryDiagnose", new(
            English: "Paste an error or log and work through it.",
            Italian: "Incolla un errore o un log e analizzalo passo dopo passo.",
            French: "Collez une erreur ou un journal et analysez-le pas à pas.",
            German: "Füge einen Fehler oder ein Protokoll ein und analysiere es Schritt für Schritt.",
            Spanish: "Pega un error o registro y analízalo paso a paso.",
            Vietnamese: "Dán lỗi hoặc nhật ký và phân tích từng bước."));
        entries.Add("Oobe.ChangeLater", new(
            English: "Your choices stay yours: hm --setup · hm --skills · hm --status.",
            Italian: "Le scelte restano tue: hm --setup · hm --skills · hm --status.",
            French: "Vos choix restent les vôtres : hm --setup · hm --skills · hm --status.",
            German: "Deine Entscheidungen bleiben deine: hm --setup · hm --skills · hm --status.",
            Spanish: "Tus decisiones siguen siendo tuyas: hm --setup · hm --skills · hm --status.",
            Vietnamese: "Các lựa chọn vẫn là của bạn: hm --setup · hm --skills · hm --status."));
    }
}
