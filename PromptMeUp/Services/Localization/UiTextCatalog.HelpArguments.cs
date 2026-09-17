// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds concrete help examples and argument meanings in every supported language.</summary>
    private static void AddHelpArgumentEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Help.Browse.DiagnoseExample", new(
            English: "dotnet build failed",
            Italian: "dotnet build non riesce",
            French: "dotnet build a échoué",
            German: "dotnet build ist fehlgeschlagen",
            Spanish: "dotnet build ha fallado",
            Vietnamese: "dotnet build thất bại"));
        entries.Add("Help.Browse.ScriptExample", new(
            English: "List the largest files in this folder",
            Italian: "Elenca i file più grandi in questa cartella",
            French: "Lister les plus gros fichiers de ce dossier",
            German: "Die größten Dateien in diesem Ordner auflisten",
            Spanish: "Lista los archivos más grandes de esta carpeta",
            Vietnamese: "Liệt kê các tệp lớn nhất trong thư mục này"));
        entries.Add("Help.Browse.PlanExample", new(
            English: "Check why the project does not build",
            Italian: "Verifica perché il progetto non compila",
            French: "Vérifier pourquoi le projet ne compile pas",
            German: "Prüfen, warum sich das Projekt nicht kompilieren lässt",
            Spanish: "Comprueba por qué el proyecto no compila",
            Vietnamese: "Kiểm tra lý do dự án không biên dịch được"));
        entries.Add("Help.Argument.Question", new(
            English: "The question to ask; quotes keep it as one argument.",
            Italian: "La domanda da porre; le virgolette la mantengono in un unico argomento.",
            French: "La question à poser ; les guillemets en font un seul argument.",
            German: "Die Frage; Anführungszeichen halten sie als ein Argument zusammen.",
            Spanish: "La pregunta; las comillas la mantienen como un solo argumento.",
            Vietnamese: "Câu hỏi cần đặt; dấu ngoặc kép giữ câu thành một đối số."));
        entries.Add("Help.Argument.Error", new(
            English: "The error message or symptom to explain.",
            Italian: "Il messaggio di errore o il sintomo da spiegare.",
            French: "Le message d'erreur ou le symptôme à expliquer.",
            German: "Die zu erklärende Fehlermeldung oder das beobachtete Problem.",
            Spanish: "El mensaje de error o síntoma que quieres explicar.",
            Vietnamese: "Thông báo lỗi hoặc dấu hiệu cần giải thích."));
        entries.Add("Help.Argument.Script", new(
            English: "What the PowerShell script should do.",
            Italian: "Che cosa deve fare lo script PowerShell.",
            French: "Ce que le script PowerShell doit faire.",
            German: "Was das PowerShell-Skript tun soll.",
            Spanish: "Lo que debe hacer el script de PowerShell.",
            Vietnamese: "Việc tập lệnh PowerShell cần thực hiện."));
        entries.Add("Help.Argument.Goal", new(
            English: "The result the guided plan should achieve.",
            Italian: "Il risultato che il piano guidato deve raggiungere.",
            French: "Le résultat que le plan guidé doit atteindre.",
            German: "Das Ziel, das der geführte Plan erreichen soll.",
            Spanish: "El resultado que debe alcanzar el plan guiado.",
            Vietnamese: "Kết quả mà kế hoạch có hướng dẫn cần đạt được."));
        entries.Add("Help.Argument.Rename", new(
            English: "Preview new names; files change only after approval.",
            Italian: "Mostra i nuovi nomi; i file cambiano solo dopo l'approvazione.",
            French: "Prévisualiser les nouveaux noms ; les fichiers ne changent qu'après accord.",
            German: "Neue Namen vorab anzeigen; Dateien ändern sich erst nach Zustimmung.",
            Spanish: "Previsualiza los nombres nuevos; los archivos solo cambian tras tu aprobación.",
            Vietnamese: "Xem trước tên mới; tệp chỉ thay đổi sau khi được chấp thuận."));
        entries.Add("Help.Argument.File", new(
            English: "Choose the file or folder to inspect.",
            Italian: "Sceglie il file o la cartella da esaminare.",
            French: "Choisir le fichier ou le dossier à examiner.",
            German: "Die zu prüfende Datei oder den Ordner auswählen.",
            Spanish: "Elige el archivo o la carpeta que se va a examinar.",
            Vietnamese: "Chọn tệp hoặc thư mục cần kiểm tra."));
        entries.Add("Help.Argument.CurrentFolder", new(
            English: "The current working folder.",
            Italian: "La cartella di lavoro corrente.",
            French: "Le dossier de travail actuel.",
            German: "Der aktuelle Arbeitsordner.",
            Spanish: "La carpeta de trabajo actual.",
            Vietnamese: "Thư mục làm việc hiện tại."));
        entries.Add("Help.Argument.Pattern", new(
            English: "Limit the preview to names matching this pattern.",
            Italian: "Limita l'anteprima ai nomi che corrispondono a questo filtro.",
            French: "Limiter l'aperçu aux noms correspondant à ce motif.",
            German: "Die Vorschau auf Namen begrenzen, die diesem Muster entsprechen.",
            Spanish: "Limita la vista previa a nombres que coincidan con este patrón.",
            Vietnamese: "Giới hạn bản xem trước ở các tên khớp với mẫu này."));
        entries.Add("Help.Argument.TextFiles", new(
            English: "Match only files with the .txt extension.",
            Italian: "Seleziona solo i file con estensione .txt.",
            French: "Sélectionner uniquement les fichiers avec l'extension .txt.",
            German: "Nur Dateien mit der Erweiterung .txt auswählen.",
            Spanish: "Selecciona solo archivos con extensión .txt.",
            Vietnamese: "Chỉ khớp các tệp có phần mở rộng .txt."));
        entries.Add("Help.Argument.Prefix", new(
            English: "Add this text before each matching file name.",
            Italian: "Aggiunge questo testo prima del nome di ogni file selezionato.",
            French: "Ajouter ce texte avant le nom de chaque fichier sélectionné.",
            German: "Diesen Text vor jeden passenden Dateinamen setzen.",
            Spanish: "Añade este texto delante del nombre de cada archivo seleccionado.",
            Vietnamese: "Thêm phần văn bản này trước tên của mỗi tệp khớp."));
        entries.Add("Help.Argument.List", new(
            English: "List saved recipes without running them.",
            Italian: "Elenca le ricette salvate senza eseguirle.",
            French: "Lister les recettes enregistrées sans les exécuter.",
            German: "Gespeicherte Rezepte auflisten, ohne sie auszuführen.",
            Spanish: "Lista las recetas guardadas sin ejecutarlas.",
            Vietnamese: "Liệt kê các công thức đã lưu mà không chạy chúng."));
        entries.Add("Help.Argument.PathStatus", new(
            English: "Check PATH registration without changing it.",
            Italian: "Controlla la registrazione nel PATH senza modificarla.",
            French: "Vérifier l'inscription dans le PATH sans la modifier.",
            German: "Den PATH-Eintrag prüfen, ohne ihn zu ändern.",
            Spanish: "Comprueba el registro en PATH sin modificarlo.",
            Vietnamese: "Kiểm tra mục đăng ký PATH mà không thay đổi."));
        entries.Add("Help.Argument.Language", new(
            English: "Use this interface language for the current invocation.",
            Italian: "Usa questa lingua per l'interfaccia durante l'esecuzione corrente.",
            French: "Utiliser cette langue d'interface pour l'exécution actuelle.",
            German: "Diese Oberflächensprache für den aktuellen Aufruf verwenden.",
            Spanish: "Usa este idioma de interfaz durante la ejecución actual.",
            Vietnamese: "Dùng ngôn ngữ giao diện này cho lần chạy hiện tại."));
        entries.Add("Help.Argument.NoAnimation", new(
            English: "Disable progress animations.",
            Italian: "Disattiva le animazioni di avanzamento.",
            French: "Désactiver les animations de progression.",
            German: "Fortschrittsanimationen deaktivieren.",
            Spanish: "Desactiva las animaciones de progreso.",
            Vietnamese: "Tắt hoạt ảnh tiến trình."));
        entries.Add("Help.Argument.NoEmoji", new(
            English: "Use plain text symbols instead of emoji.",
            Italian: "Usa simboli testuali al posto delle emoji.",
            French: "Utiliser des symboles textuels à la place des emoji.",
            German: "Textzeichen anstelle von Emojis verwenden.",
            Spanish: "Usa símbolos de texto en lugar de emojis.",
            Vietnamese: "Dùng ký hiệu văn bản thay cho biểu tượng cảm xúc."));
        entries.Add("Help.Argument.Yes", new(
            English: "Approve this font operation in advance; never approve chat commands.",
            Italian: "Approva in anticipo questa operazione sui font; non approva mai i comandi della chat.",
            French: "Approuver cette opération sur les polices à l'avance ; jamais les commandes du chat.",
            German: "Diesen Schriftvorgang vorab genehmigen; niemals Chat-Befehle genehmigen.",
            Spanish: "Aprueba esta operación de fuentes por adelantado; nunca aprueba comandos del chat.",
            Vietnamese: "Chấp thuận trước thao tác phông chữ này; không bao giờ chấp thuận lệnh trò chuyện."));
        entries.Add("Help.Argument.DryRun", new(
            English: "Preview font installation without changing the system.",
            Italian: "Mostra l'anteprima dell'installazione del font senza modificare il sistema.",
            French: "Prévisualiser l'installation de la police sans modifier le système.",
            German: "Die Schriftinstallation anzeigen, ohne das System zu ändern.",
            Spanish: "Previsualiza la instalación de la fuente sin modificar el sistema.",
            Vietnamese: "Xem trước việc cài phông chữ mà không thay đổi hệ thống."));
    }
}
