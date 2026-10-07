// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language commands UI catalog.</summary>
    /// <param name="entries">The catalog receiving localized command labels and notices.</param>
    private static void AddCommandsEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Cli.CommandConflict", new(
            English: "Commands '{0}' and '{1}' cannot be combined.",
            Italian: "I comandi '{0}' e '{1}' non possono essere combinati.",
            French: "Les commandes '{0}' et '{1}' ne peuvent pas être combinées.",
            German: "Die Befehle '{0}' und '{1}' können nicht kombiniert werden.",
            Spanish: "Los comandos '{0}' y '{1}' no se pueden combinar.",
            Vietnamese: "Không thể kết hợp các lệnh '{0}' và '{1}'."));
        entries.Add("Cli.DryRunScope", new(
            English: "--dry-run is currently supported only with --install-font.",
            Italian: "--dry-run è supportato solo insieme a --install-font.",
            French: "--dry-run est pris en charge uniquement avec --install-font.",
            German: "--dry-run wird derzeit nur mit --install-font unterstützt.",
            Spanish: "--dry-run solo se admite junto con --install-font.",
            Vietnamese: "--dry-run hiện chỉ được hỗ trợ cùng --install-font."));
        entries.Add("Cli.Invalid", new(
            English: "Invalid command line.",
            Italian: "Riga di comando non valida.",
            French: "Ligne de commande non valide.",
            German: "Ungültige Befehlszeile.",
            Spanish: "Línea de comandos no válida.",
            Vietnamese: "Dòng lệnh không hợp lệ."));
        entries.Add("Cli.NonEmptyValueRequired", new(
            English: "{0} requires a non-empty value.",
            Italian: "{0} richiede un valore non vuoto.",
            French: "{0} exige une valeur non vide.",
            German: "{0} erfordert einen nicht leeren Wert.",
            Spanish: "{0} requiere un valor no vacío.",
            Vietnamese: "{0} yêu cầu một giá trị không rỗng."));
        entries.Add("Cli.PathAction", new(
            English: "--path accepts install, remove, or status.",
            Italian: "--path accetta install, remove oppure status.",
            French: "--path accepte install, remove ou status.",
            German: "--path akzeptiert install, remove oder status.",
            Spanish: "--path acepta install, remove o status.",
            Vietnamese: "--path chấp nhận install, remove hoặc status."));
        entries.Add("Cli.PositionalConflict", new(
            English: "A positional question cannot be combined with another command.",
            Italian: "Una domanda posizionale non può essere combinata con un altro comando.",
            French: "Une question positionnelle ne peut pas être combinée avec une autre commande.",
            German: "Eine Positionsfrage kann nicht mit einem anderen Befehl kombiniert werden.",
            Spanish: "Una pregunta posicional no se puede combinar con otro comando.",
            Vietnamese: "Không thể kết hợp câu hỏi vị trí với một lệnh khác."));
        entries.Add("Cli.QueryDuplicate", new(
            English: "--query can be specified only once.",
            Italian: "--query può essere specificato una sola volta.",
            French: "--query ne peut être indiqué qu'une seule fois.",
            German: "--query darf nur einmal angegeben werden.",
            Spanish: "--query solo puede indicarse una vez.",
            Vietnamese: "Chỉ được chỉ định --query một lần."));
        entries.Add("Cli.QueryText", new(
            English: "--query requires non-empty text.",
            Italian: "--query richiede un testo non vuoto.",
            French: "--query exige un texte non vide.",
            German: "--query erfordert einen nicht leeren Text.",
            Spanish: "--query requiere un texto no vacío.",
            Vietnamese: "--query yêu cầu nội dung không rỗng."));
        entries.Add("Cli.UnknownArgument", new(
            English: "Unknown argument '{0}'. Use --help for command syntax.",
            Italian: "Argomento sconosciuto '{0}'. Usa --help per la sintassi dei comandi.",
            French: "Argument inconnu '{0}'. Utilisez --help pour la syntaxe des commandes.",
            German: "Unbekanntes Argument '{0}'. Die Befehlssyntax finden Sie unter --help.",
            Spanish: "Argumento desconocido '{0}'. Usa --help para consultar la sintaxis.",
            Vietnamese: "Đối số không xác định '{0}'. Dùng --help để xem cú pháp lệnh."));
        entries.Add("Cli.UnsupportedLanguage", new(
            English: "Unsupported language '{0}'. Use: {1}.",
            Italian: "Lingua non supportata '{0}'. Usa: {1}.",
            French: "Langue non prise en charge '{0}'. Utilisez : {1}.",
            German: "Nicht unterstützte Sprache '{0}'. Verfügbar: {1}.",
            Spanish: "Idioma no compatible '{0}'. Usa: {1}.",
            Vietnamese: "Ngôn ngữ không được hỗ trợ '{0}'. Hãy dùng: {1}."));
        entries.Add("Cli.ValueRequired", new(
            English: "{0} requires a value.",
            Italian: "{0} richiede un valore.",
            French: "{0} exige une valeur.",
            German: "{0} erfordert einen Wert.",
            Spanish: "{0} requiere un valor.",
            Vietnamese: "{0} yêu cầu một giá trị."));
        entries.Add("Command.AiReview", new(
            English: "AI review",
            Italian: "Revisione AI",
            French: "Analyse IA",
            German: "KI-Prüfung",
            Spanish: "Revisión de IA",
            Vietnamese: "Đánh giá AI"));
        entries.Add("Command.Authorize", new(
            English: "Authorize this exact command now?",
            Italian: "Autorizzare ora questo comando esatto?",
            French: "Autoriser cette commande exacte maintenant ?",
            German: "Diesen exakten Befehl jetzt autorisieren?",
            Spanish: "¿Autorizar ahora este comando exacto?",
            Vietnamese: "Cấp quyền chạy chính xác lệnh này ngay bây giờ?"));
        entries.Add("Command.Accepted", new(
            English: "Command accepted: {0}",
            Italian: "Comando accettato: {0}",
            French: "Commande acceptée : {0}",
            German: "Befehl akzeptiert: {0}",
            Spanish: "Comando aceptado: {0}",
            Vietnamese: "Đã chấp nhận lệnh: {0}"));
        entries.Add("Command.Copy", new(
            English: "Copy command without running it",
            Italian: "Copia il comando senza eseguirlo",
            French: "Copier la commande sans l’exécuter",
            German: "Befehl kopieren, ohne ihn auszuführen",
            Spanish: "Copiar el comando sin ejecutarlo",
            Vietnamese: "Sao chép lệnh mà không chạy"));
        entries.Add("Command.CopyAndExit", new(
            English: "Copy command to clipboard and exit",
            Italian: "Copia comando negli appunti e chiudi",
            French: "Copier la commande dans le presse-papiers et quitter",
            German: "Befehl in die Zwischenablage kopieren und beenden",
            Spanish: "Copiar el comando al portapapeles y salir",
            Vietnamese: "Sao chép lệnh vào bảng nhớ tạm và thoát"));
        entries.Add("Command.CopyMenu", new(
            English: "Copy…", Italian: "Copia…", French: "Copier…",
            German: "Kopieren…", Spanish: "Copiar…", Vietnamese: "Sao chép…"));
        entries.Add("Command.CopyMenuTitle", new(
            English: "Choose a command to copy to the clipboard and exit",
            Italian: "Scegli il comando da copiare negli appunti e chiudi",
            French: "Choisissez la commande à copier dans le presse-papiers et quittez",
            German: "Befehl zum Kopieren in die Zwischenablage und Beenden wählen",
            Spanish: "Elige el comando que copiar al portapapeles y salir",
            Vietnamese: "Chọn lệnh để sao chép vào bảng nhớ tạm và thoát"));
        entries.Add("Command.CopyMenuBack", new(
            English: "Back", Italian: "Indietro", French: "Retour",
            German: "Zurück", Spanish: "Volver", Vietnamese: "Quay lại"));
        entries.Add("Command.Copied", new(
            English: "Command copied. Nothing was executed.",
            Italian: "Comando copiato. Non è stato eseguito nulla.",
            French: "Commande copiée. Rien n’a été exécuté.",
            German: "Befehl kopiert. Nichts wurde ausgeführt.",
            Spanish: "Comando copiado. No se ejecutó nada.",
            Vietnamese: "Đã sao chép lệnh. Không có lệnh nào được chạy."));
        entries.Add("Command.CopyFailed", new(
            English: "Could not copy the command. Nothing was executed.",
            Italian: "Impossibile copiare il comando. Non è stato eseguito nulla.",
            French: "Impossible de copier la commande. Rien n’a été exécuté.",
            German: "Befehl konnte nicht kopiert werden. Nichts wurde ausgeführt.",
            Spanish: "No se pudo copiar el comando. No se ejecutó nada.",
            Vietnamese: "Không thể sao chép lệnh. Không có lệnh nào được chạy."));
        entries.Add("Command.Cancelled", new(
            English: "Command cancelled; nothing was executed.",
            Italian: "Comando annullato; non è stato eseguito nulla.",
            French: "Commande annulée ; rien n'a été exécuté.",
            German: "Befehl abgebrochen; nichts wurde ausgeführt.",
            Spanish: "Comando cancelado; no se ejecutó nada.",
            Vietnamese: "Đã hủy lệnh; không có gì được thực thi."));
        entries.Add("Command.Elapsed", new(
            English: "Elapsed",
            Italian: "Durata",
            French: "Durée",
            German: "Dauer",
            Spanish: "Duración",
            Vietnamese: "Thời gian"));
        entries.Add("Command.ExitCode", new(
            English: "Exit code",
            Italian: "Codice uscita",
            French: "Code de sortie",
            German: "Exitcode",
            Spanish: "Código de salida",
            Vietnamese: "Mã thoát"));
        entries.Add("Command.LocalReview", new(
            English: "Local review",
            Italian: "Revisione locale",
            French: "Analyse locale",
            German: "Lokale Prüfung",
            Spanish: "Revisión local",
            Vietnamese: "Đánh giá cục bộ"));
        entries.Add("Command.Output", new(
            English: "Command output",
            Italian: "Output comando",
            French: "Sortie de commande",
            German: "Befehlsausgabe",
            Spanish: "Salida del comando",
            Vietnamese: "Đầu ra lệnh"));
        entries.Add("Command.NoOutput", new(
            English: "The command produced no output.",
            Italian: "Il comando non ha prodotto output.",
            French: "La commande n’a produit aucune sortie.",
            German: "Der Befehl hat keine Ausgabe erzeugt.",
            Spanish: "El comando no produjo ninguna salida.",
            Vietnamese: "Lệnh không tạo đầu ra."));
        entries.Add("Command.ReportOutputProblem", new(
            English: "Report a problem with the command output",
            Italian: "Segnala un problema con l'output del comando",
            French: "Signaler un problème avec la sortie de la commande",
            German: "Ein Problem mit der Befehlsausgabe melden",
            Spanish: "Informar de un problema con la salida del comando",
            Vietnamese: "Báo cáo sự cố với đầu ra lệnh"));
        entries.Add("Command.LiveKeys", new(
            English: "Esc: stop this command · No automatic time limit",
            Italian: "Esc: interrompi questo comando · Nessun limite automatico di durata",
            French: "Échap : arrêter cette commande · Aucune limite de durée automatique",
            German: "Esc: Diesen Befehl stoppen · Kein automatisches Zeitlimit",
            Spanish: "Esc: detener este comando · Sin límite de tiempo automático",
            Vietnamese: "Esc: dừng lệnh này · Không giới hạn thời gian tự động"));
        entries.Add("Command.Interrupted", new(
            English: "interrupted", Italian: "interrotto", French: "interrompue",
            German: "abgebrochen", Spanish: "interrumpido", Vietnamese: "đã dừng"));
        entries.Add("Command.Quiet", new(
            English: "Command still running · {0} · Waiting for new output",
            Italian: "Comando ancora in esecuzione · {0} · In attesa di nuovo output",
            French: "Commande toujours en cours · {0} · En attente de nouvelle sortie",
            German: "Befehl läuft noch · {0} · Warten auf neue Ausgabe",
            Spanish: "Comando en ejecución · {0} · Esperando nueva salida",
            Vietnamese: "Lệnh vẫn đang chạy · {0} · Đang chờ đầu ra mới"));
        entries.Add("Command.OutputTruncated", new(
            English: "The retained output is incomplete; first and last excerpts are used for AI analysis.",
            Italian: "L'output conservato è incompleto; per l'analisi AI vengono usati gli estratti iniziale e finale.",
            French: "La sortie conservée est incomplète ; les extraits initial et final servent à l'analyse IA.",
            German: "Die gespeicherte Ausgabe ist unvollständig; Anfang und Ende werden für die KI-Analyse verwendet.",
            Spanish: "La salida conservada está incompleta; se usan los extractos inicial y final para el análisis de IA.",
            Vietnamese: "Đầu ra được lưu không đầy đủ; phần đầu và cuối được dùng để phân tích AI."));
        entries.Add("Command.Preview", new(
            English: "COMMAND PREVIEW // AUTHORIZATION REQUIRED",
            Italian: "ANTEPRIMA COMANDO // AUTORIZZAZIONE RICHIESTA",
            French: "APERÇU DE LA COMMANDE // AUTORISATION REQUISE",
            German: "BEFEHLSVORSCHAU // AUTORISIERUNG ERFORDERLICH",
            Spanish: "VISTA PREVIA DEL COMANDO // AUTORIZACIÓN REQUERIDA",
            Vietnamese: "XEM TRƯỚC LỆNH // CẦN CẤP QUYỀN"));
        entries.Add("Command.Risk", new(
            English: "Risk score",
            Italian: "Punteggio rischio",
            French: "Score de risque",
            German: "Risikowert",
            Spanish: "Puntuación de riesgo",
            Vietnamese: "Điểm rủi ro"));
        entries.Add("Command.Risk.Critical", new(
            English: "CRITICAL",
            Italian: "CRITICO",
            French: "CRITIQUE",
            German: "KRITISCH",
            Spanish: "CRÍTICO",
            Vietnamese: "NGHIÊM TRỌNG"));
        entries.Add("Command.Risk.High", new(
            English: "HIGH",
            Italian: "ALTO",
            French: "ÉLEVÉ",
            German: "HOCH",
            Spanish: "ALTO",
            Vietnamese: "CAO"));
        entries.Add("Command.Risk.Low", new(
            English: "LOW",
            Italian: "BASSO",
            French: "FAIBLE",
            German: "NIEDRIG",
            Spanish: "BAJO",
            Vietnamese: "THẤP"));
        entries.Add("Command.Risk.Medium", new(
            English: "MEDIUM",
            Italian: "MEDIO",
            French: "MOYEN",
            German: "MITTEL",
            Spanish: "MEDIO",
            Vietnamese: "TRUNG BÌNH"));
        entries.Add("Command.Running", new(
            English: "Running the authorized command...",
            Italian: "Esecuzione del comando autorizzato...",
            French: "Exécution de la commande autorisée...",
            German: "Autorisierter Befehl wird ausgeführt...",
            Spanish: "Ejecutando el comando autorizado...",
            Vietnamese: "Đang chạy lệnh đã được cấp quyền..."));
        entries.Add("Command.SendOutput", new(
            English: "If executed, stdout/stderr may be stored locally and sent to OpenAI in the next turn.",
            Italian: "Se eseguiti, stdout/stderr possono essere salvati in locale e inviati a OpenAI nel turno successivo.",
            French: "En cas d'exécution, stdout/stderr peuvent être enregistrés localement et envoyés à OpenAI au tour suivant.",
            German: "Bei Ausführung können stdout/stderr lokal gespeichert und in der nächsten Runde an OpenAI gesendet werden.",
            Spanish: "Si se ejecuta, stdout/stderr puede guardarse localmente y enviarse a OpenAI en el siguiente turno.",
            Vietnamese: "Nếu chạy, stdout/stderr có thể được lưu cục bộ và gửi tới OpenAI ở lượt tiếp theo."));
        entries.Add("Command.Timeout", new(
            English: "timeout",
            Italian: "timeout",
            French: "délai dépassé",
            German: "Zeitüberschreitung",
            Spanish: "tiempo agotado",
            Vietnamese: "hết thời gian"));
        entries.Add("Command.Truncated", new(
            English: "Truncated",
            Italian: "Troncato",
            French: "Tronqué",
            German: "Gekürzt",
            Spanish: "Truncado",
            Vietnamese: "Đã cắt"));
        entries.Add("CommandMenu.Choose", new(
            English: "What would you like to do?",
            Italian: "Cosa vuoi fare?",
            French: "Que souhaitez-vous faire ?",
            German: "Was möchten Sie tun?",
            Spanish: "¿Qué quieres hacer?",
            Vietnamese: "Bạn muốn làm gì?"));
        entries.Add("CommandMenu.ContinueHint", new(
            English: "Want to add a detail or ask another question? We can pick up right here, or stop for now.",
            Italian: "Vuoi aggiungere un dettaglio o fare un'altra domanda? Possiamo continuare da qui oppure fermarci.",
            French: "Vous voulez ajouter un détail ou poser une autre question ? Nous pouvons reprendre ici ou nous arrêter pour le moment.",
            German: "Möchten Sie etwas ergänzen oder noch etwas fragen? Wir können hier weitermachen oder es dabei belassen.",
            Spanish: "¿Quieres añadir un detalle o hacer otra pregunta? Podemos seguir desde aquí o parar por ahora.",
            Vietnamese: "Bạn muốn bổ sung chi tiết hay hỏi thêm? Chúng ta có thể tiếp tục từ đây hoặc dừng lại."));
        entries.Add("CommandMenu.ContinueTitle", new(
            English: "What next?",
            Italian: "Come vuoi proseguire?",
            French: "Et maintenant ?",
            German: "Wie möchten Sie weitermachen?",
            Spanish: "¿Cómo quieres seguir?",
            Vietnamese: "Bạn muốn tiếp tục thế nào?"));
        entries.Add("CommandMenu.DigitHint", new(
            English: "Press a number from 0 to {0}; no Enter needed.",
            Italian: "Premi un numero da 0 a {0}; non serve Invio.",
            French: "Appuyez sur un chiffre de 0 à {0} ; inutile d'appuyer sur Entrée.",
            German: "Drücken Sie eine Ziffer von 0 bis {0}; die Eingabetaste ist nicht nötig.",
            Spanish: "Pulsa un número del 0 al {0}; no hace falta pulsar Intro.",
            Vietnamese: "Nhấn một số từ 0 đến {0}; không cần nhấn Enter."));
        entries.Add("CommandMenu.Finish", new(
            English: "Finish here",
            Italian: "Termina qui",
            French: "Terminer ici",
            German: "Hier beenden",
            Spanish: "Terminar aquí",
            Vietnamese: "Kết thúc tại đây"));
        entries.Add("CommandMenu.Hint", new(
            English: "Suggested commands won't run on their own. Choose one to see the exact command and its risks before you decide.",
            Italian: "Nessun comando parte da solo. Scegline uno per vedere il comando esatto e i rischi prima di decidere.",
            French: "Les commandes suggérées ne se lancent pas seules. Choisissez-en une pour voir la commande exacte et ses risques avant de décider.",
            German: "Kein vorgeschlagener Befehl startet von selbst. Wählen Sie einen aus, um den genauen Befehl und seine Risiken zu sehen, bevor Sie entscheiden.",
            Spanish: "Los comandos sugeridos no se ejecutan solos. Elige uno para ver el comando exacto y sus riesgos antes de decidir.",
            Vietnamese: "Lệnh được gợi ý không tự chạy. Chọn một lệnh để xem nội dung chính xác và rủi ro trước khi quyết định."));
        entries.Add("CommandMenu.InvalidNumber", new(
            English: "Choose a number from 0 to {0}.",
            Italian: "Scegli un numero da 0 a {0}.",
            French: "Choisissez un nombre de 0 à {0}.",
            German: "Wählen Sie eine Zahl von 0 bis {0}.",
            Spanish: "Elige un número del 0 al {0}.",
            Vietnamese: "Chọn một số từ 0 đến {0}."));
        entries.Add("CommandMenu.None", new(
            English: "Do not execute commands",
            Italian: "Non eseguire comandi",
            French: "Ne pas exécuter de commandes",
            German: "Keine Befehle ausführen",
            Spanish: "No ejecutar comandos",
            Vietnamese: "Không chạy lệnh nào"));
        entries.Add("CommandMenu.NumberHint", new(
            English: "Type a number from 0 to {0}, then press Enter.",
            Italian: "Digita un numero da 0 a {0}, poi premi Invio.",
            French: "Saisissez un nombre de 0 à {0}, puis appuyez sur Entrée.",
            German: "Geben Sie eine Zahl von 0 bis {0} ein und drücken Sie die Eingabetaste.",
            Spanish: "Escribe un número del 0 al {0} y pulsa Intro.",
            Vietnamese: "Nhập một số từ 0 đến {0}, rồi nhấn Enter."));
        entries.Add("CommandMenu.StartChat", new(
            English: "Continue chatting",
            Italian: "Continua in chat",
            French: "Continuer la discussion",
            German: "Im Chat weitermachen",
            Spanish: "Seguir conversando",
            Vietnamese: "Tiếp tục trò chuyện"));
        entries.Add("CommandMenu.Title", new(
            English: "Suggested next step",
            Italian: "Prossimo passo suggerito",
            French: "Étape suivante suggérée",
            German: "Vorgeschlagener nächster Schritt",
            Spanish: "Siguiente paso sugerido",
            Vietnamese: "Bước tiếp theo được gợi ý"));
        entries.Add("Risk.Advisory", new(
            English: "The AI review is advisory: inspect the command yourself.",
            Italian: "La revisione AI è consultiva: controlla comunque il comando.",
            French: "L'avis de l'IA est consultatif : vérifiez vous-même la commande.",
            German: "Die KI-Prüfung dient als Hinweis: Prüfen Sie den Befehl selbst.",
            Spanish: "La revisión de IA es orientativa: comprueba el comando.",
            Vietnamese: "Đánh giá AI chỉ để tham khảo: hãy tự kiểm tra lệnh."));
        entries.Add("Risk.Critical", new(
            English: "Destructive, shutdown, or broad-scope operations were detected.",
            Italian: "Sono state rilevate operazioni distruttive, di arresto o ad ampio raggio.",
            French: "Des opérations destructrices, d'arrêt ou de grande portée ont été détectées.",
            German: "Destruktive Vorgänge, Herunterfahren oder weitreichende Eingriffe wurden erkannt.",
            Spanish: "Se detectaron operaciones destructivas, de apagado o de amplio alcance.",
            Vietnamese: "Phát hiện thao tác phá hủy, tắt máy hoặc ảnh hưởng diện rộng."));
        entries.Add("Risk.Description", new(
            English: "## Local review\n\n- **Detected effect:** {0}\n- **State:** preview only; the command has not run yet.",
            Italian: "## Valutazione locale\n\n- **Effetto rilevato:** {0}\n- **Stato:** anteprima soltanto; il comando non è ancora stato eseguito.",
            French: "## Évaluation locale\n\n- **Effet détecté :** {0}\n- **État :** aperçu uniquement ; la commande n'a pas encore été exécutée.",
            German: "## Lokale Bewertung\n\n- **Erkannte Wirkung:** {0}\n- **Status:** nur Vorschau; der Befehl wurde noch nicht ausgeführt.",
            Spanish: "## Evaluación local\n\n- **Efecto detectado:** {0}\n- **Estado:** solo vista previa; el comando aún no se ha ejecutado.",
            Vietnamese: "## Đánh giá cục bộ\n\n- **Tác động phát hiện:** {0}\n- **Trạng thái:** chỉ xem trước; lệnh chưa được thực thi."));
        entries.Add("Risk.High", new(
            English: "File, repository, service, or system-configuration changes were detected.",
            Italian: "Sono state rilevate modifiche a file, repository, servizi o configurazione di sistema.",
            French: "Des modifications de fichiers, dépôts, services ou paramètres système ont été détectées.",
            German: "Änderungen an Dateien, Repositorys, Diensten oder der Systemkonfiguration wurden erkannt.",
            Spanish: "Se detectaron cambios en archivos, repositorios, servicios o configuración del sistema.",
            Vietnamese: "Phát hiện thay đổi tệp, kho mã, dịch vụ hoặc cấu hình hệ thống."));
        entries.Add("Risk.LocalWins", new(
            English: "The more conservative local rule overrode the AI score.",
            Italian: "La regola locale più prudente ha prevalso sul punteggio AI.",
            French: "La règle locale plus prudente a prévalu sur le score de l'IA.",
            German: "Die vorsichtigere lokale Regel hatte Vorrang vor der KI-Bewertung.",
            Spanish: "La regla local más prudente prevaleció sobre la puntuación de IA.",
            Vietnamese: "Quy tắc cục bộ thận trọng hơn được ưu tiên hơn điểm AI."));
        entries.Add("Risk.Low", new(
            English: "The command appears primarily diagnostic or read-only.",
            Italian: "Il comando appare prevalentemente diagnostico o in sola lettura.",
            French: "La commande semble principalement diagnostique ou en lecture seule.",
            German: "Der Befehl scheint hauptsächlich zur Diagnose oder zum Lesen zu dienen.",
            Spanish: "El comando parece principalmente de diagnóstico o de solo lectura.",
            Vietnamese: "Lệnh có vẻ chủ yếu dùng để chẩn đoán hoặc chỉ đọc."));
        entries.Add("Risk.Medium", new(
            English: "Network, installation, or process-launch activity was detected.",
            Italian: "Sono state rilevate attività di rete, installazione o avvio di processi.",
            French: "Des activités réseau, d'installation ou de lancement de processus ont été détectées.",
            German: "Netzwerkaktivität, Installation oder Prozessstart wurde erkannt.",
            Spanish: "Se detectó actividad de red, instalación o inicio de procesos.",
            Vietnamese: "Phát hiện hoạt động mạng, cài đặt hoặc khởi chạy tiến trình."));
        entries.Add("Risk.Unavailable", new(
            English: "AI review unavailable; showing the local assessment.",
            Italian: "Revisione AI non disponibile; è mostrata la valutazione locale.",
            French: "Avis de l'IA indisponible ; affichage de l'évaluation locale.",
            German: "KI-Prüfung nicht verfügbar; die lokale Bewertung wird angezeigt.",
            Spanish: "Revisión de IA no disponible; se muestra la evaluación local.",
            Vietnamese: "Đánh giá AI không khả dụng; hiển thị đánh giá cục bộ."));
        entries.Add("Risk.Unknown", new(
            English: "The command may change system state; verify its arguments and path.",
            Italian: "Il comando può modificare lo stato del sistema; verifica argomenti e percorso.",
            French: "La commande peut modifier le système ; vérifiez ses arguments et son chemin.",
            German: "Der Befehl kann den Systemzustand ändern; prüfen Sie Argumente und Pfad.",
            Spanish: "El comando puede modificar el sistema; comprueba argumentos y ruta.",
            Vietnamese: "Lệnh có thể thay đổi trạng thái hệ thống; kiểm tra đối số và đường dẫn."));
    }
}
