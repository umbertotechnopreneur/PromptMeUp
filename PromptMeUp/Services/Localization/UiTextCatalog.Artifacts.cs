// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language artifacts UI catalog.</summary>
    private static void AddArtifactsEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Artifact.Configuration", new(
            English: "{0} must be a whole number of MiB between 1 and 64.",
            Italian: "{0} deve essere un numero intero di MiB tra 1 e 64.",
            French: "{0} doit être un nombre entier de Mio entre 1 et 64.",
            German: "{0} muss eine ganze Zahl von MiB zwischen 1 und 64 sein.",
            Spanish: "{0} debe ser un número entero de MiB entre 1 y 64.",
            Vietnamese: "{0} phải là số MiB nguyên từ 1 đến 64."));
        entries.Add("Artifact.OutputBudget", new(
            English: "Artifact output tokens",
            Italian: "Token output artefatti",
            French: "Jetons de sortie des artefacts",
            German: "Ausgabetokens für Artefakte",
            Spanish: "Tokens de salida de artefactos",
            Vietnamese: "Token đầu ra sản phẩm"));
        entries.Add("Artifact.OutputConfiguration", new(
            English: "{0} must be a whole number of tokens between 1 and 65,536.",
            Italian: "{0} deve essere un numero intero di token tra 1 e 65.536.",
            French: "{0} doit être un nombre entier de jetons entre 1 et 65 536.",
            German: "{0} muss eine ganze Tokenzahl zwischen 1 und 65.536 sein.",
            Spanish: "{0} debe ser un número entero de tokens entre 1 y 65.536.",
            Vietnamese: "{0} phải là số token nguyên từ 1 đến 65.536."));
        entries.Add("Artifact.PlanLimit", new(
            English: "Plan limit",
            Italian: "Limite piani",
            French: "Limite des plans",
            German: "Planlimit",
            Spanish: "Límite de planes",
            Vietnamese: "Giới hạn kế hoạch"));
        entries.Add("Artifact.ScriptLimit", new(
            English: "Script limit",
            Italian: "Limite script",
            French: "Limite des scripts",
            German: "Skriptlimit",
            Spanish: "Límite de scripts",
            Vietnamese: "Giới hạn tập lệnh"));
        entries.Add("Artifact.TooLarge", new(
            English: "Artifact exceeds the configured limit of {0:N0} UTF-8 bytes.",
            Italian: "Il contenuto supera il limite configurato di {0:N0} byte UTF-8.",
            French: "Le contenu dépasse la limite configurée de {0:N0} octets UTF-8.",
            German: "Der Inhalt überschreitet das konfigurierte Limit von {0:N0} UTF-8-Bytes.",
            Spanish: "El contenido supera el límite configurado de {0:N0} bytes UTF-8.",
            Vietnamese: "Nội dung vượt giới hạn đã cấu hình là {0:N0} byte UTF-8."));
        entries.Add("Diagnose.Help", new(
            English: "Diagnose errors and logs",
            Italian: "Diagnostica errori e log",
            French: "Diagnostiquer les erreurs et journaux",
            German: "Fehler und Protokolle diagnostizieren",
            Spanish: "Diagnosticar errores y registros",
            Vietnamese: "Chẩn đoán lỗi và nhật ký"));
        entries.Add("Input.ContextBudget", new(
            English: "Estimated input ({0:N0} tokens) exceeds the available context ({1:N0} tokens), reserving {2:N0} output tokens. Reduce the input or output budget.",
            Italian: "L'input stimato ({0:N0} token) supera il contesto disponibile ({1:N0} token), riservando {2:N0} token di output. Riduci l'input o il budget di output.",
            French: "L'entrée estimée ({0:N0} jetons) dépasse le contexte disponible ({1:N0} jetons), avec {2:N0} jetons réservés à la sortie. Réduisez l'entrée ou le budget de sortie.",
            German: "Die geschätzte Eingabe ({0:N0} Tokens) überschreitet den verfügbaren Kontext ({1:N0} Tokens), bei {2:N0} reservierten Ausgabetokens. Eingabe oder Ausgabebudget reduzieren.",
            Spanish: "La entrada estimada ({0:N0} tokens) supera el contexto disponible ({1:N0} tokens), reservando {2:N0} tokens de salida. Reduce la entrada o el presupuesto de salida.",
            Vietnamese: "Đầu vào ước tính ({0:N0} token) vượt ngữ cảnh khả dụng ({1:N0} token), dành {2:N0} token cho đầu ra. Giảm đầu vào hoặc ngân sách đầu ra."));
        entries.Add("Input.Empty", new(
            English: "Provide non-empty input.",
            Italian: "Fornisci un testo non vuoto.",
            French: "Fournissez un texte non vide.",
            German: "Geben Sie einen nicht leeren Text ein.",
            Spanish: "Proporciona texto no vacío.",
            Vietnamese: "Cung cấp văn bản không rỗng."));
        entries.Add("Input.FileError", new(
            English: "Cannot read this text file.",
            Italian: "Impossibile leggere questo file di testo.",
            French: "Impossible de lire ce fichier texte.",
            German: "Diese Textdatei kann nicht gelesen werden.",
            Spanish: "No se puede leer este archivo de texto.",
            Vietnamese: "Không thể đọc tệp văn bản này."));
        entries.Add("Input.FileOption", new(
            English: "--file requires one path and may appear once.",
            Italian: "--file richiede un percorso e può apparire una sola volta.",
            French: "--file exige un chemin et ne peut apparaître qu'une fois.",
            German: "--file benötigt einen Pfad und darf nur einmal vorkommen.",
            Spanish: "--file requiere una ruta y solo puede aparecer una vez.",
            Vietnamese: "--file cần một đường dẫn và chỉ được dùng một lần."));
        entries.Add("Input.SecretArgument", new(
            English: "Do not pass credentials as command-line arguments. Use a sanitized file or interactive input.",
            Italian: "Non passare credenziali negli argomenti. Usa un file redatto o l'input interattivo.",
            French: "Ne transmettez pas d'identifiants en arguments. Utilisez un fichier expurgé ou la saisie interactive.",
            German: "Übergeben Sie keine Zugangsdaten als Argumente. Verwenden Sie eine bereinigte Datei oder interaktive Eingabe.",
            Spanish: "No pases credenciales como argumentos. Usa un archivo depurado o entrada interactiva.",
            Vietnamese: "Không truyền thông tin xác thực qua đối số. Dùng tệp đã lọc hoặc nhập tương tác."));
        entries.Add("Input.Sharing", new(
            English: "The selected text is sent to the AI provider after recognizable credentials are redacted.",
            Italian: "Il testo selezionato viene inviato al provider AI dopo la redazione delle credenziali riconoscibili.",
            French: "Le texte sélectionné est envoyé au fournisseur IA après masquage des identifiants reconnaissables.",
            German: "Der ausgewählte Text wird nach Entfernung erkennbarer Zugangsdaten an den KI-Anbieter gesendet.",
            Spanish: "El texto seleccionado se envía al proveedor de IA tras ocultar las credenciales reconocibles.",
            Vietnamese: "Văn bản đã chọn được gửi đến nhà cung cấp AI sau khi lọc thông tin xác thực nhận diện được."));
        entries.Add("Input.SourceConflict", new(
            English: "Choose one input source: text, --file, or a pipe.",
            Italian: "Scegli una fonte: testo, --file o una pipe.",
            French: "Choisissez une source : texte, --file ou un pipe.",
            German: "Wählen Sie eine Quelle: Text, --file oder Pipe.",
            Spanish: "Elige una fuente: texto, --file o una tubería.",
            Vietnamese: "Chọn một nguồn: văn bản, --file hoặc pipe."));
        entries.Add("Input.Timeout", new(
            English: "Input did not finish within 30 seconds.",
            Italian: "L'input non è terminato entro 30 secondi.",
            French: "La lecture n'est pas terminée après 30 secondes.",
            German: "Die Eingabe wurde nicht innerhalb von 30 Sekunden abgeschlossen.",
            Spanish: "La entrada no terminó en 30 segundos.",
            Vietnamese: "Đầu vào chưa kết thúc sau 30 giây."));
        entries.Add("Input.TooLong", new(
            English: "Input exceeds {0} characters. Select a smaller excerpt.",
            Italian: "Il testo supera {0} caratteri. Seleziona un estratto più breve.",
            French: "Le texte dépasse {0} caractères. Sélectionnez un extrait plus court.",
            German: "Die Eingabe überschreitet {0} Zeichen. Wählen Sie einen kürzeren Ausschnitt.",
            Spanish: "El texto supera {0} caracteres. Selecciona un fragmento menor.",
            Vietnamese: "Đầu vào vượt quá {0} ký tự. Chọn đoạn ngắn hơn."));
        entries.Add("Plan.Busy", new(
            English: "This plan is already open in another process.",
            Italian: "Questo piano è già aperto in un altro processo.",
            French: "Ce plan est déjà ouvert dans un autre processus.",
            German: "Dieser Plan ist bereits in einem anderen Prozess geöffnet.",
            Spanish: "Este plan ya está abierto en otro proceso.",
            Vietnamese: "Kế hoạch đang mở trong tiến trình khác."));
        entries.Add("Plan.Completed", new(
            English: "Completed and confirmed",
            Italian: "Completato e confermato",
            French: "Terminé et confirmé",
            German: "Abgeschlossen und bestätigt",
            Spanish: "Completado y confirmado",
            Vietnamese: "Đã hoàn thành và xác nhận"));
        entries.Add("Plan.Directory", new(
            English: "Resume from the original directory: {0}",
            Italian: "Riprendi dalla cartella originale: {0}",
            French: "Reprenez depuis le dossier original : {0}",
            German: "Im ursprünglichen Verzeichnis fortsetzen: {0}",
            Spanish: "Reanuda desde el directorio original: {0}",
            Vietnamese: "Tiếp tục từ thư mục gốc: {0}"));
        entries.Add("Plan.Depth", new(
            English: "Plans deeper than ten steps are not supported yet.",
            Italian: "I piani con più di dieci passi non sono ancora supportati.",
            French: "Les plans de plus de dix étapes ne sont pas encore pris en charge.",
            German: "Pläne mit mehr als zehn Schritten werden noch nicht unterstützt.",
            Spanish: "Los planes de más de diez pasos aún no son compatibles.",
            Vietnamese: "Kế hoạch có hơn mười bước hiện chưa được hỗ trợ."));
        entries.Add("Plan.Help", new(
            English: "Guide and resume a plan",
            Italian: "Guida e riprendi un piano",
            French: "Guider et reprendre un plan",
            German: "Plan begleiten und fortsetzen",
            Spanish: "Guiar y reanudar un plan",
            Vietnamese: "Hướng dẫn và tiếp tục kế hoạch"));
        entries.Add("Plan.Invalid", new(
            English: "Invalid or unsupported plan. Require 1–10 bounded steps with commands and verification.",
            Italian: "Piano non valido o non supportato. Servono 1–10 passi limitati con comandi e verifica.",
            French: "Plan invalide ou non pris en charge. Il faut 1 à 10 étapes bornées avec commandes et vérification.",
            German: "Ungültiger oder nicht unterstützter Plan. Erforderlich sind 1–10 begrenzte Schritte mit Befehlen und Prüfung.",
            Spanish: "Plan inválido o no compatible. Se requieren 1–10 pasos acotados con comandos y verificación.",
            Vietnamese: "Kế hoạch không hợp lệ hoặc không hỗ trợ. Cần 1–10 bước có giới hạn với lệnh và kiểm tra."));
        entries.Add("Plan.LoadError", new(
            English: "Cannot load this saved plan.",
            Italian: "Impossibile caricare questo piano salvato.",
            French: "Impossible de charger ce plan enregistré.",
            German: "Gespeicherter Plan kann nicht geladen werden.",
            Spanish: "No se puede cargar este plan guardado.",
            Vietnamese: "Không thể tải kế hoạch đã lưu."));
        entries.Add("Plan.NeedsReview", new(
            English: "Needs verification",
            Italian: "Richiede verifica",
            French: "Vérification requise",
            German: "Prüfung erforderlich",
            Spanish: "Requiere verificación",
            Vietnamese: "Cần kiểm tra"));
        entries.Add("Plan.Outcome", new(
            English: "Does the observed result match: {0}?",
            Italian: "Il risultato osservato corrisponde a: {0}?",
            French: "Le résultat observé correspond-il à : {0} ?",
            German: "Entspricht das beobachtete Ergebnis: {0}?",
            Spanish: "¿El resultado observado coincide con: {0}?",
            Vietnamese: "Kết quả quan sát có khớp với: {0}?"));
        entries.Add("Plan.Pending", new(
            English: "Pending",
            Italian: "Da eseguire",
            French: "En attente",
            German: "Ausstehend",
            Spanish: "Pendiente",
            Vietnamese: "Chờ"));
        entries.Add("Plan.Recheck", new(
            English: "Verify current state before continuing. The original action will not be repeated.",
            Italian: "Verifica lo stato attuale prima di continuare. L'azione originale non verrà ripetuta.",
            French: "Vérifiez l'état actuel avant de continuer. L'action originale ne sera pas répétée.",
            German: "Prüfen Sie den aktuellen Zustand. Die ursprüngliche Aktion wird nicht wiederholt.",
            Spanish: "Verifica el estado actual antes de continuar. La acción original no se repetirá.",
            Vietnamese: "Kiểm tra trạng thái hiện tại trước khi tiếp tục. Thao tác gốc sẽ không lặp lại."));
        entries.Add("Plan.Resume", new(
            English: "To resume this plan, use this command:",
            Italian: "Per riprendere questo piano, usa questo comando:",
            French: "Pour reprendre ce plan, utilisez cette commande :",
            German: "Diesen Plan mit diesem Befehl fortsetzen:",
            Spanish: "Para reanudar este plan, usa este comando:",
            Vietnamese: "Để tiếp tục kế hoạch này, dùng lệnh sau:"));
        entries.Add("Plan.Running", new(
            English: "Outcome unknown",
            Italian: "Esito sconosciuto",
            French: "Résultat inconnu",
            German: "Ergebnis unbekannt",
            Spanish: "Resultado desconocido",
            Vietnamese: "Kết quả chưa rõ"));
        entries.Add("Plan.Start", new(
            English: "Start guidance? Each command will be reviewed separately.",
            Italian: "Avviare la guida? Ogni comando verrà valutato separatamente.",
            French: "Démarrer le guide ? Chaque commande sera vérifiée séparément.",
            German: "Anleitung starten? Jeder Befehl wird einzeln geprüft.",
            Spanish: "¿Iniciar la guía? Cada comando se revisará por separado.",
            Vietnamese: "Bắt đầu hướng dẫn? Mỗi lệnh sẽ được xem xét riêng."));
        entries.Add("Plan.Status", new(
            English: "Status",
            Italian: "Stato",
            French: "État",
            German: "Status",
            Spanish: "Estado",
            Vietnamese: "Trạng thái"));
        entries.Add("Plan.Step", new(
            English: "Step and expected result",
            Italian: "Passo e risultato atteso",
            French: "Étape et résultat attendu",
            German: "Schritt und erwartetes Ergebnis",
            Spanish: "Paso y resultado esperado",
            Vietnamese: "Bước và kết quả mong đợi"));
        entries.Add("Plan.Stopped", new(
            English: "Plan paused: resolve the discrepancy, then resume to verify. No further action was started.",
            Italian: "Piano sospeso: risolvi la discrepanza, poi riprendi per verificare. Nessuna azione successiva è stata avviata.",
            French: "Plan suspendu : résolvez l'écart, puis reprenez pour vérifier. Aucune action suivante n'a démarré.",
            German: "Plan pausiert: Abweichung beheben und zur Prüfung fortsetzen. Keine weitere Aktion wurde gestartet.",
            Spanish: "Plan pausado: resuelve la discrepancia y reanuda para verificar. No se inició otra acción.",
            Vietnamese: "Kế hoạch tạm dừng: xử lý sai lệch rồi tiếp tục kiểm tra. Chưa bắt đầu thao tác tiếp theo."));
        entries.Add("Plan.Usage", new(
            English: "Use --plan <goal> or --plan --resume <id>.",
            Italian: "Usa --plan <obiettivo> oppure --plan --resume <id>.",
            French: "Utilisez --plan <objectif> ou --plan --resume <id>.",
            German: "Verwenden Sie --plan <Ziel> oder --plan --resume <id>.",
            Spanish: "Usa --plan <objetivo> o --plan --resume <id>.",
            Vietnamese: "Dùng --plan <mục-tiêu> hoặc --plan --resume <id>."));
        entries.Add("Preview.Bytes", new(
            English: "Bytes",
            Italian: "Byte",
            French: "Octets",
            German: "Bytes",
            Spanish: "Bytes",
            Vietnamese: "Byte"));
        entries.Add("Preview.Changed", new(
            English: "The snapshot changed. Generate a new preview before continuing.",
            Italian: "Lo stato è cambiato. Genera una nuova anteprima prima di continuare.",
            French: "L'état a changé. Créez un nouvel aperçu avant de continuer.",
            German: "Der Zustand hat sich geändert. Neue Vorschau erstellen.",
            Spanish: "El estado cambió. Genera otra vista previa antes de continuar.",
            Vietnamese: "Trạng thái đã thay đổi. Tạo bản xem trước mới trước khi tiếp tục."));
        entries.Add("Preview.Collision", new(
            English: "Collision: nothing will run.",
            Italian: "Collisione: nessuna esecuzione.",
            French: "Collision : aucune exécution.",
            German: "Kollision: keine Ausführung.",
            Spanish: "Colisión: no se ejecutará nada.",
            Vietnamese: "Xung đột: không chạy gì."));
        entries.Add("Preview.Delete", new(
            English: "Delete file",
            Italian: "Elimina file",
            French: "Supprimer le fichier",
            German: "Datei löschen",
            Spanish: "Eliminar archivo",
            Vietnamese: "Xóa tệp"));
        entries.Add("Preview.Destination", new(
            English: "Choose an existing destination directory.",
            Italian: "Scegli una cartella di destinazione esistente.",
            French: "Choisissez un dossier de destination existant.",
            German: "Vorhandenes Zielverzeichnis wählen.",
            Spanish: "Elige un directorio de destino existente.",
            Vietnamese: "Chọn thư mục đích đã tồn tại."));
        entries.Add("Preview.Empty", new(
            English: "No matching files.",
            Italian: "Nessun file corrispondente.",
            French: "Aucun fichier correspondant.",
            German: "Keine passenden Dateien.",
            Spanish: "No hay archivos coincidentes.",
            Vietnamese: "Không có tệp phù hợp."));
        entries.Add("Preview.Help", new(
            English: "Preview file effects",
            Italian: "Anteprima degli effetti sui file",
            French: "Aperçu des effets sur les fichiers",
            German: "Dateiauswirkungen anzeigen",
            Spanish: "Vista previa de efectos en archivos",
            Vietnamese: "Xem trước tác động lên tệp"));
        entries.Add("Preview.Links", new(
            English: "Symbolic links and reparse points are not supported by this preview.",
            Italian: "Questa anteprima non supporta link simbolici o reparse point.",
            French: "Cet aperçu ne prend pas en charge les liens symboliques et points de réanalyse.",
            German: "Symbolische Links und Analysepunkte werden nicht unterstützt.",
            Spanish: "Esta vista previa no admite enlaces simbólicos ni puntos de reanálisis.",
            Vietnamese: "Bản xem trước không hỗ trợ liên kết tượng trưng và reparse point."));
        entries.Add("Preview.ReadError", new(
            English: "Cannot inspect these file paths.",
            Italian: "Impossibile ispezionare questi percorsi.",
            French: "Impossible d'inspecter ces chemins.",
            German: "Diese Pfade können nicht geprüft werden.",
            Spanish: "No se pueden inspeccionar estas rutas.",
            Vietnamese: "Không thể kiểm tra các đường dẫn này."));
        entries.Add("Preview.Ready", new(
            English: "Snapshot checked",
            Italian: "Stato controllato",
            French: "État vérifié",
            German: "Zustand geprüft",
            Spanish: "Estado comprobado",
            Vietnamese: "Đã kiểm tra trạng thái"));
        entries.Add("Preview.Review", new(
            English: "Review each command for separate approval?",
            Italian: "Rivedere ogni comando per una conferma separata?",
            French: "Examiner chaque commande pour un accord distinct ?",
            German: "Jeden Befehl einzeln zur Genehmigung prüfen?",
            Spanish: "¿Revisar cada comando para aprobación separada?",
            Vietnamese: "Xem từng lệnh để cấp quyền riêng?"));
        entries.Add("Preview.Snapshot", new(
            English: "Local snapshot only; other processes can still change files. No recursive directories or arbitrary shell commands are simulated.",
            Italian: "Solo stato locale corrente; altri processi possono ancora cambiare i file. Non vengono simulati alberi ricorsivi o comandi shell arbitrari.",
            French: "Instantané local ; d'autres processus peuvent encore modifier les fichiers. Aucune simulation de dossiers récursifs ou commandes arbitraires.",
            German: "Lokale Momentaufnahme; andere Prozesse können Dateien weiter ändern. Keine Simulation rekursiver Verzeichnisse oder beliebiger Shell-Befehle.",
            Spanish: "Solo estado local actual; otros procesos pueden cambiar archivos. No se simulan directorios recursivos ni comandos arbitrarios.",
            Vietnamese: "Chỉ ảnh chụp trạng thái cục bộ; tiến trình khác vẫn có thể thay đổi tệp. Không mô phỏng thư mục đệ quy hay lệnh shell tùy ý."));
        entries.Add("Preview.Source", new(
            English: "Source",
            Italian: "Sorgente",
            French: "Source",
            German: "Quelle",
            Spanish: "Origen",
            Vietnamese: "Nguồn"));
        entries.Add("Preview.Target", new(
            English: "Destination / effect",
            Italian: "Destinazione / effetto",
            French: "Destination / effet",
            German: "Ziel / Auswirkung",
            Spanish: "Destino / efecto",
            Vietnamese: "Đích / tác động"));
        entries.Add("Preview.TooMany", new(
            English: "Preview limit reached ({0} files). Narrow the source or filter.",
            Italian: "Limite anteprima raggiunto ({0} file). Restringi la sorgente o il filtro.",
            French: "Limite atteinte ({0} fichiers). Réduisez la source ou le filtre.",
            German: "Vorschaulimit erreicht ({0} Dateien). Quelle oder Filter eingrenzen.",
            Spanish: "Límite de vista previa alcanzado ({0} archivos). Reduce el origen o filtro.",
            Vietnamese: "Đạt giới hạn xem trước ({0} tệp). Thu hẹp nguồn hoặc bộ lọc."));
        entries.Add("Preview.Total", new(
            English: "{0} files · {1:N0} bytes",
            Italian: "{0} file · {1:N0} byte",
            French: "{0} fichiers · {1:N0} octets",
            German: "{0} Dateien · {1:N0} Bytes",
            Spanish: "{0} archivos · {1:N0} bytes",
            Vietnamese: "{0} tệp · {1:N0} byte"));
        entries.Add("Preview.Usage", new(
            English: "Use --preview copy|move|rename|delete --file <source>; copy/move require --output <existing-directory>, rename requires --prefix <text>. Optional: --pattern <glob>.",
            Italian: "Usa --preview copy|move|rename|delete --file <sorgente>; copy/move richiedono --output <cartella-esistente>, rename richiede --prefix <testo>. Facoltativo: --pattern <glob>.",
            French: "Utilisez --preview copy|move|rename|delete --file <source> ; copy/move exigent --output <dossier-existant>, rename exige --prefix <texte>. Option : --pattern <glob>.",
            German: "Nutzen Sie --preview copy|move|rename|delete --file <Quelle>; copy/move benötigen --output <vorhandenes-Verzeichnis>, rename benötigt --prefix <Text>. Optional: --pattern <glob>.",
            Spanish: "Usa --preview copy|move|rename|delete --file <origen>; copy/move requieren --output <directorio-existente>, rename requiere --prefix <texto>. Opcional: --pattern <glob>.",
            Vietnamese: "Dùng --preview copy|move|rename|delete --file <nguồn>; copy/move cần --output <thư-mục-có-sẵn>, rename cần --prefix <văn-bản>. Tùy chọn: --pattern <glob>."));
        entries.Add("Script.Action", new(
            English: "Choose the next step",
            Italian: "Scegli il prossimo passo",
            French: "Choisissez la prochaine étape",
            German: "Nächsten Schritt wählen",
            Spanish: "Elige el siguiente paso",
            Vietnamese: "Chọn bước tiếp theo"));
        entries.Add("Script.DoNothing", new(
            English: "Do nothing",
            Italian: "Non fare nulla",
            French: "Ne rien faire",
            German: "Nichts tun",
            Spanish: "No hacer nada",
            Vietnamese: "Không làm gì"));
        entries.Add("Script.Confirm", new(
            English: "Save the displayed source to {0}?",
            Italian: "Salvare il codice mostrato in {0}?",
            French: "Enregistrer le code affiché dans {0} ?",
            German: "Angezeigten Quelltext in {0} speichern?",
            Spanish: "¿Guardar el código mostrado en {0}?",
            Vietnamese: "Lưu mã đã hiển thị vào {0}?"));
        entries.Add("Script.Help", new(
            English: "Create or revise a {0} script",
            Italian: "Crea o modifica uno script {0}",
            French: "Créer ou modifier un script {0}",
            German: "{0}-Skript erstellen oder überarbeiten",
            Spanish: "Crear o modificar un script de {0}",
            Vietnamese: "Tạo hoặc sửa tập lệnh {0}"));
        entries.Add("Script.Invalid", new(
            English: "Invalid script: require non-empty source up to 12000 characters without credentials or redaction markers.",
            Italian: "Script non valido: serve codice non vuoto, entro 12000 caratteri, senza credenziali o marcatori di redazione.",
            French: "Script invalide : code non vide, 12000 caractères maximum, sans identifiants ni marqueurs de masquage.",
            German: "Ungültiges Skript: nicht leer, höchstens 12000 Zeichen, ohne Zugangsdaten oder Schwärzungsmarker.",
            Spanish: "Script inválido: código no vacío de hasta 12000 caracteres sin credenciales ni marcas de ocultación.",
            Vietnamese: "Tập lệnh không hợp lệ: cần mã không rỗng, tối đa 12000 ký tự, không chứa thông tin xác thực hay dấu đã lọc."));
        entries.Add("Script.OutputOption", new(
            English: "--output requires one new script destination with the selected language extension.",
            Italian: "--output richiede una nuova destinazione script con l'estensione del linguaggio selezionato.",
            French: "--output exige une nouvelle destination de script avec l'extension du langage choisi.",
            German: "--output benötigt ein neues Skriptziel mit der Erweiterung der gewählten Sprache.",
            Spanish: "--output requiere un destino de script nuevo con la extensión del lenguaje elegido.",
            Vietnamese: "--output cần một đích tập lệnh mới với phần mở rộng của ngôn ngữ đã chọn."));
        entries.Add("Script.OutputDestination", new(
            English: "Requested save destination: {0}",
            Italian: "Destinazione di salvataggio richiesta: {0}",
            French: "Destination d'enregistrement demandée : {0}",
            German: "Angefordertes Speicherziel: {0}",
            Spanish: "Destino de guardado solicitado: {0}",
            Vietnamese: "Đích lưu đã yêu cầu: {0}"));
        entries.Add("Script.SuggestedDestination", new(
            English: "Suggested new local file: {0}",
            Italian: "Nuovo file locale suggerito: {0}",
            French: "Nouveau fichier local suggéré : {0}",
            German: "Vorgeschlagene neue lokale Datei: {0}",
            Spanish: "Nuevo archivo local sugerido: {0}",
            Vietnamese: "Tệp cục bộ mới được đề xuất: {0}"));
        entries.Add("Script.RuntimeUnavailable", new(
            English: "{0} is not available on this computer. You can still review or save the draft.",
            Italian: "{0} non è disponibile su questo computer. Puoi comunque rivedere o salvare la bozza.",
            French: "{0} n'est pas disponible sur cet ordinateur. Vous pouvez toujours relire ou enregistrer le brouillon.",
            German: "{0} ist auf diesem Computer nicht verfügbar. Sie können den Entwurf trotzdem prüfen oder speichern.",
            Spanish: "{0} no está disponible en este equipo. Aún puedes revisar o guardar el borrador.",
            Vietnamese: "{0} không có trên máy tính này. Bạn vẫn có thể xem lại hoặc lưu bản nháp."));
        entries.Add("Script.Execute", new(
            English: "Run once from a temporary file",
            Italian: "Esegui una volta da un file temporaneo",
            French: "Exécuter une fois depuis un fichier temporaire",
            German: "Einmal aus einer temporären Datei ausführen",
            Spanish: "Ejecutar una vez desde un archivo temporal",
            Vietnamese: "Chạy một lần từ tệp tạm thời"));
        entries.Add("Script.Language.PowerShell", new(
            English: "PowerShell 7",
            Italian: "PowerShell 7",
            French: "PowerShell 7",
            German: "PowerShell 7",
            Spanish: "PowerShell 7",
            Vietnamese: "PowerShell 7"));
        entries.Add("Script.Language.Batch", new(
            English: "Batch / CMD",
            Italian: "Batch / CMD",
            French: "Batch / CMD",
            German: "Batch / CMD",
            Spanish: "Batch / CMD",
            Vietnamese: "Batch / CMD"));
        entries.Add("Script.Language.Bash", new(
            English: "Bash",
            Italian: "Bash",
            French: "Bash",
            German: "Bash",
            Spanish: "Bash",
            Vietnamese: "Bash"));
        entries.Add("Script.Language.Python", new(
            English: "Python 3",
            Italian: "Python 3",
            French: "Python 3",
            German: "Python 3",
            Spanish: "Python 3",
            Vietnamese: "Python 3"));
        entries.Add("Script.Language.JavaScript", new(
            English: "JavaScript / Node.js",
            Italian: "JavaScript / Node.js",
            French: "JavaScript / Node.js",
            German: "JavaScript / Node.js",
            Spanish: "JavaScript / Node.js",
            Vietnamese: "JavaScript / Node.js"));
        entries.Add("Script.Revise", new(
            English: "Request a revision",
            Italian: "Richiedi una modifica",
            French: "Demander une modification",
            German: "Überarbeitung anfordern",
            Spanish: "Solicitar una modificación",
            Vietnamese: "Yêu cầu chỉnh sửa"));
        entries.Add("Script.Revision", new(
            English: "Describe the change",
            Italian: "Descrivi la modifica",
            French: "Décrivez la modification",
            German: "Änderung beschreiben",
            Spanish: "Describe el cambio",
            Vietnamese: "Mô tả thay đổi"));
        entries.Add("Script.Save", new(
            English: "Save to a new file",
            Italian: "Salva in un nuovo file",
            French: "Enregistrer dans un nouveau fichier",
            German: "In neuer Datei speichern",
            Spanish: "Guardar en un archivo nuevo",
            Vietnamese: "Lưu vào tệp mới"));
        entries.Add("Script.SaveError", new(
            English: "Cannot save: choose a writable destination that does not already exist.",
            Italian: "Salvataggio impossibile: scegli una destinazione scrivibile che non esista già.",
            French: "Échec de sauvegarde : choisissez une destination accessible qui n'existe pas.",
            German: "Speichern fehlgeschlagen: Wählen Sie ein beschreibbares, noch nicht vorhandenes Ziel.",
            Spanish: "No se puede guardar: elige un destino escribible que no exista.",
            Vietnamese: "Không thể lưu: chọn đường dẫn ghi được và chưa tồn tại."));
        entries.Add("Script.Saved", new(
            English: "Saved: {0}",
            Italian: "Salvato: {0}",
            French: "Enregistré : {0}",
            German: "Gespeichert: {0}",
            Spanish: "Guardado: {0}",
            Vietnamese: "Đã lưu: {0}"));
        entries.Add("Script.Source", new(
            English: "Full source",
            Italian: "Codice completo",
            French: "Code complet",
            German: "Vollständiger Quelltext",
            Spanish: "Código completo",
            Vietnamese: "Toàn bộ mã"));
        entries.Add("Script.Usage", new(
            English: "Use --script <request> [--file <source>] [--output <new-script>]. Choose the preferred language in setup.",
            Italian: "Usa --script <richiesta> [--file <sorgente>] [--output <nuovo-script>]. Scegli il linguaggio preferito nelle impostazioni.",
            French: "Utilisez --script <demande> [--file <source>] [--output <nouveau-script>]. Choisissez le langage préféré dans les paramètres.",
            German: "Verwenden Sie --script <Anfrage> [--file <Quelle>] [--output <neues-Skript>]. Wählen Sie die bevorzugte Sprache in den Einstellungen.",
            Spanish: "Usa --script <petición> [--file <origen>] [--output <nuevo-script>]. Elige el lenguaje preferido en la configuración.",
            Vietnamese: "Dùng --script <yêu-cầu> [--file <nguồn>] [--output <tập-lệnh-mới>]. Chọn ngôn ngữ ưu tiên trong phần cài đặt."));
        entries.Add("Script.Validate", new(
            English: "Review the syntax-check command",
            Italian: "Rivedi il comando di controllo sintassi",
            French: "Vérifier la commande d'analyse syntaxique",
            German: "Befehl zur Syntaxprüfung prüfen",
            Spanish: "Revisar el comando de validación sintáctica",
            Vietnamese: "Xem lệnh kiểm tra cú pháp"));
        entries.Add("Script.ValidationNote", new(
            English: "Syntax checks do not prove correctness or safety. For PowerShell, AnalyzerAvailable reports whether optional PSScriptAnalyzer was used.",
            Italian: "Il controllo sintattico non dimostra correttezza o sicurezza. Per PowerShell, AnalyzerAvailable indica se è stato usato PSScriptAnalyzer opzionale.",
            French: "Les contrôles syntaxiques ne prouvent ni la correction ni la sécurité. Pour PowerShell, AnalyzerAvailable indique l'utilisation du module facultatif PSScriptAnalyzer.",
            German: "Syntaxprüfungen beweisen weder Korrektheit noch Sicherheit. Bei PowerShell zeigt AnalyzerAvailable die Nutzung des optionalen PSScriptAnalyzer.",
            Spanish: "Las comprobaciones de sintaxis no garantizan corrección ni seguridad. Para PowerShell, AnalyzerAvailable indica si se usó PSScriptAnalyzer opcional.",
            Vietnamese: "Kiểm tra cú pháp không chứng minh tính đúng đắn hay an toàn. Với PowerShell, AnalyzerAvailable cho biết có dùng PSScriptAnalyzer tùy chọn hay không."));
    }
}
