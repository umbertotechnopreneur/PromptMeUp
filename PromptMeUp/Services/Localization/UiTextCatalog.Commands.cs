// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language commands UI catalog.</summary>
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
            English: "Add details or answer the assistant's question. The conversation keeps your original request and this answer.",
            Italian: "Aggiungi dettagli o rispondi alla domanda dell'assistente. La conversazione conserva la richiesta originale e questa risposta.",
            French: "Ajoutez des précisions ou répondez à la question de l'assistant. La conversation conserve votre demande initiale et cette réponse.",
            German: "Ergänzen Sie Details oder beantworten Sie die Frage des Assistenten. Ihre ursprüngliche Anfrage und diese Antwort bleiben im Gespräch erhalten.",
            Spanish: "Añade detalles o responde a la pregunta del asistente. La conversación conserva tu solicitud original y esta respuesta.",
            Vietnamese: "Bổ sung chi tiết hoặc trả lời câu hỏi của trợ lý. Cuộc trò chuyện giữ lại yêu cầu ban đầu và câu trả lời này."));
        entries.Add("CommandMenu.ContinueTitle", new(
            English: "Continue the conversation",
            Italian: "Continua la conversazione",
            French: "Continuer la conversation",
            German: "Gespräch fortsetzen",
            Spanish: "Continuar la conversación",
            Vietnamese: "Tiếp tục cuộc trò chuyện"));
        entries.Add("CommandMenu.Finish", new(
            English: "Finish here",
            Italian: "Termina qui",
            French: "Terminer ici",
            German: "Hier beenden",
            Spanish: "Terminar aquí",
            Vietnamese: "Kết thúc tại đây"));
        entries.Add("CommandMenu.Hint", new(
            English: "Suggestions never run automatically. Select a command only to inspect its exact preview and risk check.",
            Italian: "I suggerimenti non vengono mai eseguiti automaticamente. Seleziona un comando solo per ispezionarne anteprima esatta e rischio.",
            French: "Les suggestions ne s'exécutent jamais automatiquement. Sélectionnez une commande uniquement pour examiner son aperçu exact et son risque.",
            German: "Vorschläge werden nie automatisch ausgeführt. Wählen Sie einen Befehl nur zur Prüfung der exakten Vorschau und des Risikos.",
            Spanish: "Las sugerencias nunca se ejecutan automáticamente. Elige un comando solo para inspeccionar su vista previa exacta y riesgo.",
            Vietnamese: "Gợi ý không bao giờ tự chạy. Chỉ chọn lệnh để xem trước chính xác và kiểm tra rủi ro."));
        entries.Add("CommandMenu.None", new(
            English: "Do not execute commands",
            Italian: "Non eseguire comandi",
            French: "Ne pas exécuter de commandes",
            German: "Keine Befehle ausführen",
            Spanish: "No ejecutar comandos",
            Vietnamese: "Không chạy lệnh nào"));
        entries.Add("CommandMenu.StartChat", new(
            English: "Clarify the request or continue in chat",
            Italian: "Chiarisci la richiesta o continua in chat",
            French: "Préciser la demande ou continuer le chat",
            German: "Anfrage präzisieren oder im Chat fortfahren",
            Spanish: "Aclarar la solicitud o continuar en el chat",
            Vietnamese: "Làm rõ yêu cầu hoặc tiếp tục chat"));
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
