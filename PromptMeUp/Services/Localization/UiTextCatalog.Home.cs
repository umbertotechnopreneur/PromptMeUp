// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the home menu and optional desktop launcher copy in all six supported languages.</summary>
    private static void AddHomeEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Logs.Prepared", new(
            English: "Support ZIP created on your desktop: {0} ({1} log files).",
            Italian: "ZIP per l’assistenza creato sul desktop: {0} ({1} file di log).",
            French: "ZIP d’assistance créé sur votre bureau : {0} ({1} fichiers journaux).",
            German: "Support-ZIP auf dem Desktop erstellt: {0} ({1} Protokolldateien).",
            Spanish: "ZIP de soporte creado en el escritorio: {0} ({1} archivos de registro).",
            Vietnamese: "Đã tạo ZIP hỗ trợ trên màn hình nền: {0} ({1} tệp nhật ký)."));
        entries.Add("Logs.ReviewBeforeSending", new(
            English: "Nothing was sent. Inspect the ZIP, then attach it to your support email.",
            Italian: "Non è stato inviato nulla. Controlla lo ZIP, poi allegalo all’email per l’assistenza.",
            French: "Rien n’a été envoyé. Vérifiez le ZIP, puis joignez-le à votre e-mail d’assistance.",
            German: "Es wurde nichts gesendet. Prüfe das ZIP und hänge es dann an deine Support-E-Mail an.",
            Spanish: "No se ha enviado nada. Revisa el ZIP y adjúntalo al correo de soporte.",
            Vietnamese: "Chưa có gì được gửi. Hãy kiểm tra ZIP rồi đính kèm vào email hỗ trợ."));
        entries.Add("Startup.Failed", new(
            English: "PromptMeUp could not start ({0}). Diagnostic logs: {1}",
            Italian: "PromptMeUp non è riuscito ad avviarsi ({0}). Log diagnostici: {1}",
            French: "PromptMeUp n’a pas pu démarrer ({0}). Journaux de diagnostic : {1}",
            German: "PromptMeUp konnte nicht starten ({0}). Diagnoseprotokolle: {1}",
            Spanish: "PromptMeUp no pudo iniciarse ({0}). Registros de diagnóstico: {1}",
            Vietnamese: "PromptMeUp không thể khởi động ({0}). Nhật ký chẩn đoán: {1}"));
        entries.Add("Startup.PressEnter", new(
            English: "Press Enter to close. You can send the diagnostic logs to support.",
            Italian: "Premi Invio per chiudere. Puoi inviare i log diagnostici all’assistenza.",
            French: "Appuyez sur Entrée pour fermer. Vous pouvez envoyer les journaux à l’assistance.",
            German: "Drücke Enter zum Schließen. Du kannst die Diagnoseprotokolle an den Support senden.",
            Spanish: "Pulsa Intro para cerrar. Puedes enviar los registros al soporte.",
            Vietnamese: "Nhấn Enter để đóng. Bạn có thể gửi nhật ký chẩn đoán cho bộ phận hỗ trợ."));
        entries.Add("Home.CheckboxHelp", new(
            English: "Space selects or clears the checkbox. Enter continues.",
            Italian: "Spazio seleziona o deseleziona la casella. Invio continua.",
            French: "Espace coche ou décoche la case. Entrée continue.",
            German: "Leertaste aktiviert oder deaktiviert das Kästchen. Eingabetaste fährt fort.",
            Spanish: "Espacio marca o desmarca la casilla. Intro continúa.",
            Vietnamese: "Phím cách chọn hoặc bỏ chọn. Enter để tiếp tục."));
        entries.Add("Home.Question", new(
            English: "What would you like to do?",
            Italian: "Cosa vuoi fare?",
            French: "Que souhaitez-vous faire ?",
            German: "Was möchtest du tun?",
            Spanish: "¿Qué quieres hacer?",
            Vietnamese: "Bạn muốn làm gì?"));
        entries.Add("Home.StarPrompt", new(
            English: "If you like PromptMeUp, give it a star on GitHub.",
            Italian: "Se ti piace PromptMeUp, lascia una stella su GitHub.",
            French: "Si PromptMeUp vous plaît, laissez-lui une étoile sur GitHub.",
            German: "Wenn dir PromptMeUp gefällt, gib dem Projekt einen Stern auf GitHub.",
            Spanish: "Si te gusta PromptMeUp, dale una estrella en GitHub.",
            Vietnamese: "Nếu bạn thích PromptMeUp, hãy tặng dự án một ngôi sao trên GitHub."));
        entries.Add("Home.Hint", new(
            English: "Press a number. No Enter needed. Esc or 0 exits.",
            Italian: "Premi un numero, senza Invio. Esc oppure 0 per uscire.",
            French: "Appuyez sur un chiffre, sans Entrée. Échap ou 0 pour quitter.",
            German: "Drücke eine Zahl, ohne Eingabetaste. Esc oder 0 beendet das Menü.",
            Spanish: "Pulsa un número, sin Intro. Esc o 0 para salir.",
            Vietnamese: "Nhấn một số, không cần Enter. Esc hoặc 0 để thoát."));
        entries.Add("Home.Chat", new(
            English: "Talk about a command",
            Italian: "Chattare riguardo a un comando",
            French: "Parler d’une commande",
            German: "Über einen Befehl sprechen",
            Spanish: "Hablar sobre un comando",
            Vietnamese: "Trao đổi về một lệnh"));
        entries.Add("Home.ChatHint", new(
            English: "Ask a question or work through a task together.",
            Italian: "Fai una domanda o affrontiamo insieme un’attività.",
            French: "Posez une question ou avançons ensemble sur une tâche.",
            German: "Stelle eine Frage oder löse eine Aufgabe gemeinsam.",
            Spanish: "Haz una pregunta o resolvamos una tarea juntos.",
            Vietnamese: "Đặt câu hỏi hoặc cùng xử lý một tác vụ."));
        entries.Add("Home.Script", new(
            English: "Write a script",
            Italian: "Scrivere uno script",
            French: "Écrire un script",
            German: "Ein Skript schreiben",
            Spanish: "Escribir un script",
            Vietnamese: "Viết một script"));
        entries.Add("Home.ScriptHint", new(
            English: "Describe what you want to automate.",
            Italian: "Descrivi cosa vuoi automatizzare.",
            French: "Décrivez ce que vous souhaitez automatiser.",
            German: "Beschreibe, was du automatisieren möchtest.",
            Spanish: "Describe qué quieres automatizar.",
            Vietnamese: "Mô tả điều bạn muốn tự động hóa."));
        entries.Add("Home.Diagnose", new(
            English: "Understand an error",
            Italian: "Capire e risolvere un errore",
            French: "Comprendre une erreur",
            German: "Einen Fehler verstehen",
            Spanish: "Entender y resolver un error",
            Vietnamese: "Hiểu và xử lý lỗi"));
        entries.Add("Home.DiagnoseHint", new(
            English: "Paste the error and get help finding the cause.",
            Italian: "Incolla l’errore e troviamo la causa.",
            French: "Collez l’erreur pour en chercher la cause.",
            German: "Füge die Fehlermeldung ein und finde die Ursache.",
            Spanish: "Pega el error y busquemos la causa.",
            Vietnamese: "Dán thông báo lỗi để tìm nguyên nhân."));
        entries.Add("Home.Memories", new(
            English: "View your memories",
            Italian: "Guardare le memorie",
            French: "Consulter les mémoires",
            German: "Erinnerungen ansehen",
            Spanish: "Ver las memorias",
            Vietnamese: "Xem bộ nhớ"));
        entries.Add("Home.MemoriesHint", new(
            English: "Review and manage what PromptMeUp remembers.",
            Italian: "Rivedi e gestisci cosa ricorda PromptMeUp.",
            French: "Vérifiez et gérez ce que PromptMeUp retient.",
            German: "Prüfe und verwalte, was PromptMeUp sich merkt.",
            Spanish: "Revisa y gestiona lo que recuerda PromptMeUp.",
            Vietnamese: "Xem lại và quản lý điều PromptMeUp ghi nhớ."));
        entries.Add("Home.Skills", new(
            English: "Work with skills",
            Italian: "Lavorare sulle skills",
            French: "Gérer les skills",
            German: "Skills verwalten",
            Spanish: "Trabajar con skills",
            Vietnamese: "Làm việc với skills"));
        entries.Add("Home.SkillsHint", new(
            English: "Discover and enable instructions for specific tasks.",
            Italian: "Scopri e abilita istruzioni per attività specifiche.",
            French: "Découvrez et activez des instructions pour des tâches précises.",
            German: "Entdecke und aktiviere Anweisungen für bestimmte Aufgaben.",
            Spanish: "Descubre y activa instrucciones para tareas concretas.",
            Vietnamese: "Khám phá và bật hướng dẫn cho tác vụ cụ thể."));
        entries.Add("Home.Settings", new(
            English: "Change preferences",
            Italian: "Cambiare le preferenze",
            French: "Modifier les préférences",
            German: "Einstellungen ändern",
            Spanish: "Cambiar preferencias",
            Vietnamese: "Đổi tùy chọn"));
        entries.Add("Home.SettingsHint", new(
            English: "Language, appearance, AI and privacy choices.",
            Italian: "Lingua, aspetto, AI e scelte sulla privacy.",
            French: "Langue, apparence, IA et confidentialité.",
            German: "Sprache, Aussehen, KI und Datenschutz.",
            Spanish: "Idioma, aspecto, IA y privacidad.",
            Vietnamese: "Ngôn ngữ, giao diện, AI và quyền riêng tư."));
        entries.Add("Home.Help", new(
            English: "Get help",
            Italian: "Consultare la guida",
            French: "Consulter l’aide",
            German: "Hilfe öffnen",
            Spanish: "Consultar la ayuda",
            Vietnamese: "Xem trợ giúp"));
        entries.Add("Home.HelpHint", new(
            English: "Find commands and practical examples.",
            Italian: "Trova comandi ed esempi pratici.",
            French: "Trouvez les commandes et des exemples pratiques.",
            German: "Finde Befehle und praktische Beispiele.",
            Spanish: "Encuentra comandos y ejemplos prácticos.",
            Vietnamese: "Tìm lệnh và ví dụ thực tế."));
        entries.Add("Home.Exit", new(
            English: "Exit",
            Italian: "Esci",
            French: "Quitter",
            German: "Beenden",
            Spanish: "Salir",
            Vietnamese: "Thoát"));
        entries.Add("Home.Redirected", new(
            English: "Open hm in an interactive terminal to use this menu. For the command guide, run hm --help.",
            Italian: "Apri hm in un terminale interattivo per usare il menu. Per la guida ai comandi, esegui hm --help.",
            French: "Ouvrez hm dans un terminal interactif pour utiliser ce menu. Guide des commandes : hm --help.",
            German: "Öffne hm in einem interaktiven Terminal. Die Befehlsübersicht findest du mit hm --help.",
            Spanish: "Abre hm en un terminal interactivo para usar este menú. Guía de comandos: hm --help.",
            Vietnamese: "Mở hm trong terminal tương tác để dùng menu. Hướng dẫn lệnh: hm --help."));
        entries.Add("Home.ScriptRequest", new(
            English: "What should the script do?",
            Italian: "Cosa deve fare lo script?",
            French: "Que doit faire le script ?",
            German: "Was soll das Skript tun?",
            Spanish: "¿Qué debe hacer el script?",
            Vietnamese: "Script cần làm gì?"));
        entries.Add("Home.ErrorRequest", new(
            English: "Paste the error you want to understand",
            Italian: "Incolla l’errore che vuoi capire",
            French: "Collez l’erreur à comprendre",
            German: "Füge die Fehlermeldung ein",
            Spanish: "Pega el error que quieres entender",
            Vietnamese: "Dán lỗi bạn muốn tìm hiểu"));
        entries.Add("Home.InputHelp", new(
            English: "Leave empty to return to the menu.",
            Italian: "Lascia vuoto per tornare al menu.",
            French: "Laissez vide pour revenir au menu.",
            German: "Leer lassen, um zum Menü zurückzukehren.",
            Spanish: "Déjalo vacío para volver al menú.",
            Vietnamese: "Để trống để quay lại menu."));
        entries.Add("Home.Desktop", new(
            English: "Add PromptMeUp to your desktop?",
            Italian: "Aggiungere PromptMeUp al desktop?",
            French: "Ajouter PromptMeUp au bureau ?",
            German: "PromptMeUp zum Desktop hinzufügen?",
            Spanish: "¿Añadir PromptMeUp al escritorio?",
            Vietnamese: "Thêm PromptMeUp vào màn hình nền?"));
        entries.Add("Home.DesktopHelp", new(
            English: "A desktop icon opens hm in your Windows default terminal. Select the checkbox to add it.",
            Italian: "Un’icona sul desktop apre hm nel terminale predefinito di Windows. Seleziona la casella per aggiungerla.",
            French: "Une icône ouvre hm dans le terminal Windows par défaut. Cochez la case pour l’ajouter au bureau.",
            German: "Ein Desktop-Symbol öffnet hm im Windows-Standardterminal. Aktiviere das Kästchen, um es hinzuzufügen.",
            Spanish: "Un icono abre hm en el terminal predeterminado de Windows. Marca la casilla para añadirlo al escritorio.",
            Vietnamese: "Biểu tượng trên màn hình nền mở hm trong terminal mặc định của Windows. Chọn ô để thêm biểu tượng."));
        entries.Add("Home.DesktopCreated", new(
            English: "Desktop shortcut created. You can open PromptMeUp with a double-click.",
            Italian: "Collegamento sul desktop creato. Ora puoi aprire PromptMeUp con un doppio clic.",
            French: "Raccourci créé. Ouvrez PromptMeUp d’un double-clic.",
            German: "Desktop-Verknüpfung erstellt. Öffne PromptMeUp mit einem Doppelklick.",
            Spanish: "Acceso directo creado. Abre PromptMeUp con doble clic.",
            Vietnamese: "Đã tạo lối tắt. Nhấp đúp để mở PromptMeUp."));
        entries.Add("Home.DesktopExists", new(
            English: "A PromptMeUp desktop shortcut already exists. It has been left unchanged.",
            Italian: "Sul desktop esiste già un collegamento PromptMeUp. È stato mantenuto.",
            French: "Un raccourci PromptMeUp existe déjà sur le bureau. Il est conservé.",
            German: "Eine PromptMeUp-Verknüpfung existiert bereits und bleibt unverändert.",
            Spanish: "Ya existe un acceso directo de PromptMeUp. Se ha conservado.",
            Vietnamese: "Lối tắt PromptMeUp đã tồn tại và được giữ nguyên."));
        entries.Add("Home.DesktopFailed", new(
            English: "The desktop shortcut could not be created. You can still start the app with hm.",
            Italian: "Non è stato possibile creare il collegamento sul desktop. Puoi comunque avviare l’app con hm.",
            French: "Impossible de créer le raccourci. Vous pouvez toujours lancer l’application avec hm.",
            German: "Die Desktop-Verknüpfung konnte nicht erstellt werden. Du kannst die App weiterhin mit hm starten.",
            Spanish: "No se pudo crear el acceso directo. Puedes iniciar la aplicación con hm.",
            Vietnamese: "Không thể tạo lối tắt. Bạn vẫn có thể mở ứng dụng bằng hm."));
    }
}
