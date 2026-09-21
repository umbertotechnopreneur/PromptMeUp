// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language conversation UI catalog.</summary>
    private static void AddConversationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Chat.SessionSummaryUnavailable", new(
            English: "The final session summary could not be refreshed. Totals are unavailable.",
            Italian: "Impossibile aggiornare lo specchietto finale della sessione. I totali non sono disponibili.",
            French: "Impossible d'actualiser le résumé final de la session. Les totaux sont indisponibles.",
            German: "Die abschließende Sitzungsübersicht konnte nicht aktualisiert werden. Summen sind nicht verfügbar.",
            Spanish: "No se pudo actualizar el resumen final de la sesión. Los totales no están disponibles.",
            Vietnamese: "Không thể cập nhật bảng tóm tắt cuối phiên. Tổng số liệu không khả dụng."));
        entries.Add("Chat.Assistant", new(
            English: "PromptMeUp",
            Italian: "PromptMeUp",
            French: "PromptMeUp",
            German: "PromptMeUp",
            Spanish: "PromptMeUp",
            Vietnamese: "PromptMeUp"));
        entries.Add("Chat.CommandsHeading", new(
            English: "Chat commands:",
            Italian: "Comandi chat:",
            French: "Commandes du chat :",
            German: "Chat-Befehle:",
            Spanish: "Comandos del chat:",
            Vietnamese: "Lệnh trò chuyện:"));
        entries.Add("Chat.Cleared", new(
            English: "Conversation cleared.",
            Italian: "Conversazione azzerata.",
            French: "Conversation effacée.",
            German: "Gespräch gelöscht.",
            Spanish: "Conversación borrada.",
            Vietnamese: "Đã xóa hội thoại."));
        entries.Add("Chat.DisplayHint", new(
            English: "The session summary appears when the workflow ends. Use /status now, or ask to show it after every visible AI result in all workflows.",
            Italian: "Lo specchietto sessione compare al termine del flusso. Usa /status ora, o chiedi di mostrarlo dopo ogni risultato AI visibile in tutti i flussi.",
            French: "Le résumé apparaît à la fin du flux. Utilisez /status maintenant ou demandez à l'afficher après chaque résultat IA visible dans tous les flux.",
            German: "Die Sitzungsübersicht erscheint am Ende des Ablaufs. Mit /status sofort anzeigen oder nach jedem sichtbaren KI-Ergebnis in allen Abläufen anfordern.",
            Spanish: "El resumen aparece al terminar el flujo. Usa /status ahora o pide que se muestre después de cada resultado visible de IA en todos los flujos.",
            Vietnamese: "Bảng tóm tắt phiên xuất hiện khi quy trình kết thúc. Dùng /status ngay hoặc yêu cầu hiển thị sau mỗi kết quả AI thấy được trong mọi quy trình."));
        entries.Add("Chat.MultilineHint", new(
            English: "Paste keeps line breaks. {0}send · {1}new line · {2}edit · {3}cancel.",
            Italian: "L'incolla conserva gli a capo. {0}invia · {1}a capo · {2}modifica · {3}annulla.",
            French: "Le collage conserve les sauts de ligne. {0}envoyer · {1}nouvelle ligne · {2}modifier · {3}annuler.",
            German: "Einfügen behält Zeilenumbrüche. {0}senden · {1}neue Zeile · {2}bearbeiten · {3}abbrechen.",
            Spanish: "Pegar conserva los saltos de línea. {0}enviar · {1}nueva línea · {2}editar · {3}cancelar.",
            Vietnamese: "Dán giữ nguyên xuống dòng. {0}gửi · {1}xuống dòng · {2}sửa · {3}hủy."));
        entries.Add("Chat.InputShortHint", new(
            English: "{0}send · {1}new line · {3}cancel",
            Italian: "{0}invia · {1}a capo · {3}annulla",
            French: "{0}envoyer · {1}nouvelle ligne · {3}annuler",
            German: "{0}senden · {1}neue Zeile · {3}abbrechen",
            Spanish: "{0}enviar · {1}nueva línea · {3}cancelar",
            Vietnamese: "{0}gửi · {1}xuống dòng · {3}hủy"));
        entries.Add("Chat.Key.Enter", new("Enter", "Invio", "Entrée", "Eingabe", "Intro", "Enter"));
        entries.Add("Chat.Key.Newline", new("Shift+Enter", "Shift+Invio", "Maj+Entrée", "Umschalt+Eingabe", "Mayús+Intro", "Shift+Enter"));
        entries.Add("Chat.Key.Arrows", new("Arrows", "Frecce", "Flèches", "Pfeile", "Flechas", "Phím mũi tên"));
        entries.Add("Chat.Key.Escape", new("Esc", "Esc", "Échap", "Esc", "Esc", "Esc"));
        entries.Add("Chat.InputLine", new(
            English: "Line {0} of {1} · Enter to send",
            Italian: "Riga {0} di {1} · Invio per inviare",
            French: "Ligne {0} sur {1} · Entrée pour envoyer",
            German: "Zeile {0} von {1} · Eingabe zum Senden",
            Spanish: "Línea {0} de {1} · Intro para enviar",
            Vietnamese: "Dòng {0} trên {1} · Enter để gửi"));
        entries.Add("Chat.PasteUnavailable", new(
            English: "Interactive text input needs an ANSI terminal with bracketed paste support. For diagnostics, you can also use --input-file.",
            Italian: "L'input interattivo richiede un terminale ANSI con supporto all'incolla delimitato. Per la diagnostica puoi anche usare --input-file.",
            French: "La saisie interactive nécessite un terminal ANSI prenant en charge le collage encadré. Pour le diagnostic, vous pouvez aussi utiliser --input-file.",
            German: "Die interaktive Eingabe benötigt ein ANSI-Terminal mit Unterstützung für geklammertes Einfügen. Zur Diagnose ist auch --input-file möglich.",
            Spanish: "La entrada interactiva necesita un terminal ANSI con soporte de pegado delimitado. Para diagnóstico también puedes usar --input-file.",
            Vietnamese: "Nhập tương tác cần terminal ANSI hỗ trợ dán có dấu phân định. Khi chẩn đoán, bạn cũng có thể dùng --input-file."));
        entries.Add("Chat.SessionSummaryShown", new(
            English: "Session summary **shown after every visible AI result** in all workflows.",
            Italian: "Specchietto sessione **visibile dopo ogni risultato AI** in tutti i flussi.",
            French: "Résumé de session **affiché après chaque résultat IA visible** dans tous les flux.",
            German: "Sitzungsübersicht wird **nach jedem sichtbaren KI-Ergebnis** in allen Abläufen angezeigt.",
            Spanish: "Resumen de sesión **visible después de cada resultado de IA** en todos los flujos.",
            Vietnamese: "**Hiện bảng tóm tắt phiên sau mỗi kết quả AI thấy được** trong mọi quy trình."));
        entries.Add("Chat.SessionSummaryHidden", new(
            English: "Session summary appears **only when a workflow ends**. View it now with `/status`.",
            Italian: "Lo specchietto sessione compare **solo al termine del flusso**. Puoi vederlo ora con `/status`.",
            French: "Le résumé de session apparaît **uniquement à la fin d'un flux**. Consultez-le maintenant avec `/status`.",
            German: "Die Sitzungsübersicht erscheint **nur am Ende eines Ablaufs**. Mit `/status` jetzt anzeigen.",
            Spanish: "El resumen de sesión aparece **solo al terminar un flujo**. Consúltalo ahora con `/status`.",
            Vietnamese: "Bảng tóm tắt phiên chỉ xuất hiện **khi quy trình kết thúc**. Xem ngay bằng `/status`."));
        entries.Add("Chat.ExecutionConfirmationRequired", new(
            English: "Every suggested command now requires your confirmation. `--direct` can still override this global preference for the current session.",
            Italian: "Ogni comando suggerito ora richiede la tua conferma. `--direct` può ancora ignorare questa preferenza globale per la sessione corrente.",
            French: "Chaque commande suggérée requiert désormais votre confirmation. `--direct` peut encore remplacer cette préférence globale pour la session en cours.",
            German: "Jeder vorgeschlagene Befehl erfordert jetzt deine Bestätigung. `--direct` kann diese globale Einstellung für die aktuelle Sitzung weiterhin überschreiben.",
            Spanish: "Cada comando sugerido requiere ahora tu confirmación. `--direct` todavía puede sustituir esta preferencia global para la sesión actual.",
            Vietnamese: "Mỗi lệnh được gợi ý giờ đều cần bạn xác nhận. `--direct` vẫn có thể ghi đè tùy chọn toàn cục này cho phiên hiện tại."));
        entries.Add("Chat.ExecutionConfirmationSavedForLater", new(
            English: "Manual confirmation is now the global default. `--direct` remains active for this session.",
            Italian: "La conferma manuale è ora l'impostazione globale. `--direct` resta attivo per questa sessione.",
            French: "La confirmation manuelle est maintenant le réglage global. `--direct` reste actif pour cette session.",
            German: "Manuelle Bestätigung ist jetzt die globale Einstellung. `--direct` bleibt für diese Sitzung aktiv.",
            Spanish: "La confirmación manual es ahora la opción global. `--direct` permanece activo durante esta sesión.",
            Vietnamese: "Xác nhận thủ công hiện là tùy chọn toàn cục. `--direct` vẫn hoạt động trong phiên này."));
        entries.Add("Chat.DirectExecutionEnabled", new(
            English: "Direct execution is now the global default for questions and chat. Eligible commands still receive local and AI risk checks, then a cancellable countdown.",
            Italian: "L'esecuzione diretta è ora l'impostazione globale per domande e chat. I comandi idonei ricevono comunque controlli di rischio locali e AI, poi un countdown annullabile.",
            French: "L'exécution directe est maintenant le réglage global pour les questions et le chat. Les commandes admissibles reçoivent toujours des contrôles de risque locaux et IA, puis un délai annulable.",
            German: "Direkte Ausführung ist jetzt die globale Einstellung für Fragen und Chats. Geeignete Befehle erhalten weiterhin lokale und KI-Risikoprüfungen, danach folgt ein abbrechbarer Countdown.",
            Spanish: "La ejecución directa es ahora la opción global para preguntas y chat. Los comandos aptos siguen recibiendo controles de riesgo locales y de IA, y después una cuenta atrás cancelable.",
            Vietnamese: "Thực thi trực tiếp hiện là tùy chọn toàn cục cho câu hỏi và chat. Các lệnh đủ điều kiện vẫn được kiểm tra rủi ro cục bộ và AI, sau đó có đếm ngược có thể hủy."));
        entries.Add("Chat.CommandSuggestionsShown", new(
            English: "Command menu and previews **shown** in this chat.",
            Italian: "Menu e anteprime dei comandi **visibili** in questa chat.",
            French: "Menu et aperçus des commandes **affichés** dans ce chat.",
            German: "Befehlsmenü und Vorschauen in diesem Chat **eingeblendet**.",
            Spanish: "Menú y vistas previas de comandos **visibles** en este chat.",
            Vietnamese: "**Đã hiện** menu và phần xem trước lệnh trong cuộc trò chuyện này."));
        entries.Add("Chat.CommandSuggestionsHidden", new(
            English: "Command menu and previews **hidden** in this chat. `/run` still requires an exact preview and approval.",
            Italian: "Menu e anteprime dei comandi **nascosti** in questa chat. `/run` richiede sempre anteprima esatta e approvazione.",
            French: "Menu et aperçus des commandes **masqués** dans ce chat. `/run` exige toujours un aperçu exact et une approbation.",
            German: "Befehlsmenü und Vorschauen in diesem Chat **ausgeblendet**. `/run` erfordert weiterhin eine genaue Vorschau und Freigabe.",
            Spanish: "Menú y vistas previas de comandos **ocultos** en este chat. `/run` sigue exigiendo una vista previa exacta y aprobación.",
            Vietnamese: "**Đã ẩn** menu và phần xem trước lệnh trong cuộc trò chuyện này. `/run` vẫn yêu cầu xem trước chính xác và phê duyệt."));
        entries.Add("Chat.Command.Clear", new(
            English: "Clear the active conversation context.",
            Italian: "Azzera il contesto attivo della conversazione.",
            French: "Efface le contexte actif de la conversation.",
            German: "Löscht den aktiven Gesprächskontext.",
            Spanish: "Borra el contexto activo de la conversación.",
            Vietnamese: "Xóa ngữ cảnh hội thoại đang hoạt động."));
        entries.Add("Chat.Command.Context", new(
            English: "Inspect active context and token budgets",
            Italian: "Mostra contesto attivo e budget token",
            French: "Voir le contexte actif et les budgets de jetons",
            German: "Aktiven Kontext und Tokenbudgets anzeigen",
            Spanish: "Ver contexto activo y presupuestos de tokens",
            Vietnamese: "Xem ngữ cảnh hiện tại và ngân sách token"));
        entries.Add("Chat.Command.Costs", new(
            English: "Show usage estimates and current model prices.",
            Italian: "Mostra le stime d'uso e i prezzi correnti dei modelli.",
            French: "Affiche les estimations d'utilisation et les prix actuels des modèles.",
            German: "Zeigt Nutzungsschätzungen und aktuelle Modellpreise.",
            Spanish: "Muestra estimaciones de uso y precios actuales de los modelos.",
            Vietnamese: "Hiển thị ước tính sử dụng và giá mô hình hiện tại."));
        entries.Add("Chat.Command.Exit", new(
            English: "Close the interactive chat.",
            Italian: "Chiude la chat interattiva.",
            French: "Ferme le chat interactif.",
            German: "Schließt den interaktiven Chat.",
            Spanish: "Cierra el chat interactivo.",
            Vietnamese: "Đóng chat tương tác."));
        entries.Add("Chat.Command.Forget", new(
            "Delete a saved memory",
            "Elimina un ricordo salvato",
            "Supprimer un souvenir enregistré",
            "Gespeicherte Erinnerung löschen",
            "Eliminar un recuerdo guardado",
            "Xóa ghi nhớ đã lưu"));
        entries.Add("Chat.Command.Memories", new(
            "List saved memories",
            "Elenca i ricordi salvati",
            "Lister les souvenirs enregistrés",
            "Gespeicherte Erinnerungen auflisten",
            "Listar recuerdos guardados",
            "Liệt kê ghi nhớ đã lưu"));
        entries.Add("Chat.Command.Remember", new(
            "Save a note for future conversations",
            "Salva una nota per le prossime conversazioni",
            "Enregistrer une note pour les prochaines conversations",
            "Notiz für künftige Gespräche speichern",
            "Guardar una nota para futuras conversaciones",
            "Lưu ghi chú cho các cuộc trò chuyện sau"));
        entries.Add("Chat.Command.RememberSyntax", new(
            "/remember <text>",
            "/remember <testo>",
            "/remember <texte>",
            "/remember <Text>",
            "/remember <texto>",
            "/remember <văn-bản>"));
        entries.Add("Chat.Command.Run", new(
            English: "Preview and check a command, then use direct countdown or manual confirmation.",
            Italian: "Mostra e verifica un comando, poi usa il countdown direct o la conferma manuale.",
            French: "Affiche et vérifie une commande, puis utilise le délai direct ou la confirmation manuelle.",
            German: "Zeigt und prüft einen Befehl, dann Direkt-Countdown oder manuelle Bestätigung.",
            Spanish: "Muestra y verifica un comando, luego usa la cuenta atrás directa o la confirmación manual.",
            Vietnamese: "Hiện và kiểm tra lệnh, rồi đếm ngược trực tiếp hoặc xác nhận thủ công."));
        entries.Add("Chat.Command.RunSyntax", new(
            English: "/run <command>",
            Italian: "/run <comando>",
            French: "/run <commande>",
            German: "/run <Befehl>",
            Spanish: "/run <comando>",
            Vietnamese: "/run <lệnh>"));
        entries.Add("Chat.Command.Status", new(
            English: "Show model, tokens, cache, and session cost.",
            Italian: "Mostra modello, token, cache e costo della sessione.",
            French: "Affiche le modèle, les jetons, le cache et le coût de la session.",
            German: "Zeigt Modell, Token, Cache und Sitzungskosten.",
            Spanish: "Muestra modelo, tokens, caché y coste de la sesión.",
            Vietnamese: "Hiển thị mô hình, token, bộ nhớ đệm và chi phí phiên."));
        entries.Add("Chat.ContextLimit", new(
            English: "The request exceeds the available context. Shorten it, use /clear, or remove notes with /forget; the chat is still open.",
            Italian: "La richiesta supera il contesto disponibile. Accorciala, usa /clear o rimuovi note con /forget; la chat resta aperta.",
            French: "La demande dépasse le contexte disponible. Raccourcissez-la, utilisez /clear ou retirez des notes avec /forget ; la conversation reste ouverte.",
            German: "Die Anfrage überschreitet den verfügbaren Kontext. Kürzen Sie sie, nutzen Sie /clear oder entfernen Sie Notizen mit /forget; der Chat bleibt geöffnet.",
            Spanish: "La solicitud supera el contexto disponible. Acórtala, usa /clear o elimina notas con /forget; el chat sigue abierto.",
            Vietnamese: "Yêu cầu vượt quá ngữ cảnh hiện có. Hãy rút ngắn, dùng /clear hoặc xóa ghi chú bằng /forget; cuộc trò chuyện vẫn mở."));
        entries.Add("Chat.Exit", new(
            English: "Chat closed.",
            Italian: "Chat chiusa.",
            French: "Chat fermé.",
            German: "Chat geschlossen.",
            Spanish: "Chat cerrado.",
            Vietnamese: "Đã đóng chat."));
        entries.Add("Chat.Hint", new(
            English: "Commands: /run <command>, /exit, /clear, /costs, /status",
            Italian: "Comandi: /run <comando>, /exit, /clear, /costs, /status",
            French: "Commandes : /run <commande>, /exit, /clear, /costs, /status",
            German: "Befehle: /run <Befehl>, /exit, /clear, /costs, /status",
            Spanish: "Comandos: /run <comando>, /exit, /clear, /costs, /status",
            Vietnamese: "Lệnh: /run <lệnh>, /exit, /clear, /costs, /status"));
        entries.Add("Chat.MemoryHint", new(
            English: "Memories in chat: {0} save · {1} list · {2} delete",
            Italian: "Memorie in chat: {0} salva · {1} elenco · {2} elimina",
            French: "Mémoires en chat : {0} enregistrer · {1} lister · {2} supprimer",
            German: "Erinnerungen im Chat: {0} speichern · {1} auflisten · {2} löschen",
            Spanish: "Memorias en chat: {0} guardar · {1} listar · {2} eliminar",
            Vietnamese: "Ghi nhớ trong chat: {0} lưu · {1} liệt kê · {2} xóa"));
        entries.Add("Chat.InputCount", new(
            English: "{0:N0} / {1:N0} characters · {2:N0} remaining",
            Italian: "{0:N0} / {1:N0} caratteri · {2:N0} disponibili",
            French: "{0:N0} / {1:N0} caractères · {2:N0} restants",
            German: "{0:N0} / {1:N0} Zeichen · {2:N0} verbleibend",
            Spanish: "{0:N0} / {1:N0} caracteres · {2:N0} restantes",
            Vietnamese: "{0:N0} / {1:N0} ký tự · còn {2:N0}"));
        entries.Add("Chat.InputTooLong", new(
            English: "Maximum length: {0:N0} characters.",
            Italian: "Lunghezza massima: {0:N0} caratteri.",
            French: "Longueur maximale : {0:N0} caractères.",
            German: "Maximale Länge: {0:N0} Zeichen.",
            Spanish: "Longitud máxima: {0:N0} caracteres.",
            Vietnamese: "Độ dài tối đa: {0:N0} ký tự."));
        entries.Add("Chat.Pruned", new(
            English: "Active context removed {0} old message(s); the audit ledger remains complete.",
            Italian: "Il contesto attivo ha rimosso {0} messaggi precedenti; il registro di audit resta completo.",
            French: "Le contexte actif a retiré {0} ancien(s) message(s) ; le journal d'audit reste complet.",
            German: "Der aktive Kontext hat {0} ältere Nachricht(en) entfernt; das Audit-Protokoll bleibt vollständig.",
            Spanish: "El contexto activo eliminó {0} mensaje(s) anterior(es); el registro de auditoría sigue completo.",
            Vietnamese: "Ngữ cảnh hoạt động đã xóa {0} tin nhắn cũ; nhật ký kiểm toán vẫn đầy đủ."));
        entries.Add("Chat.RunRequired", new(
            English: "/run requires a command.",
            Italian: "/run richiede un comando.",
            French: "/run nécessite une commande.",
            German: "/run benötigt einen Befehl.",
            Spanish: "/run requiere un comando.",
            Vietnamese: "/run cần có một lệnh."));
        entries.Add("Chat.Title", new(
            English: "Interactive chat",
            Italian: "Chat interattiva",
            French: "Chat IA interactif",
            German: "Interaktiver Chat",
            Spanish: "Chat de IA interactivo",
            Vietnamese: "Chat tương tác"));
        entries.Add("Chat.You", new(
            English: "You",
            Italian: "Tu",
            French: "Vous",
            German: "Sie",
            Spanish: "Tú",
            Vietnamese: "Bạn"));
        entries.Add("Context.InvalidBudget", new(
            English: "Context budget must be a whole number of tokens between {0:N0} and {1:N0}.",
            Italian: "Il budget del contesto deve essere un numero intero di token tra {0:N0} e {1:N0}.",
            French: "Le budget de contexte doit être un nombre entier de jetons entre {0:N0} et {1:N0}.",
            German: "Das Kontextbudget muss eine ganze Tokenzahl zwischen {0:N0} und {1:N0} sein.",
            Spanish: "El presupuesto de contexto debe ser un número entero de tokens entre {0:N0} y {1:N0}.",
            Vietnamese: "Ngân sách ngữ cảnh phải là số token nguyên từ {0:N0} đến {1:N0}."));
        entries.Add("Memory.Empty", new(
            English: "Provide a non-empty memory note.",
            Italian: "Fornisci una nota di memoria non vuota.",
            French: "Fournissez une note de mémoire non vide.",
            German: "Geben Sie eine nicht leere Erinnerungsnotiz ein.",
            Spanish: "Proporciona una nota de memoria no vacía.",
            Vietnamese: "Nhập nội dung ghi nhớ không rỗng."));
        entries.Add("Memory.Forgotten", new(
            "Memory deleted.",
            "Ricordo eliminato.",
            "Souvenir supprimé.",
            "Erinnerung gelöscht.",
            "Recuerdo eliminado.",
            "Đã xóa ghi nhớ."));
        entries.Add("Memory.Invalid", new(
            English: "Saved memory data is invalid and cannot be loaded.",
            Italian: "I dati delle memorie salvate non sono validi e non possono essere caricati.",
            French: "Les données des mémoires enregistrées sont invalides et ne peuvent pas être chargées.",
            German: "Gespeicherte Erinnerungsdaten sind ungültig und können nicht geladen werden.",
            Spanish: "Los datos de memoria guardados no son válidos y no se pueden cargar.",
            Vietnamese: "Dữ liệu ghi nhớ đã lưu không hợp lệ và không thể nạp."));
        entries.Add("Memory.InvalidId", new(
            English: "Provide one valid saved memory ID.",
            Italian: "Fornisci un ID valido di una memoria salvata.",
            French: "Fournissez un identifiant valide de mémoire enregistrée.",
            German: "Geben Sie eine gültige ID einer gespeicherten Erinnerung an.",
            Spanish: "Proporciona un ID válido de memoria guardada.",
            Vietnamese: "Nhập một ID ghi nhớ đã lưu hợp lệ."));
        entries.Add("Memory.Limit", new(
            "You already have {0:N0} saved memories. Delete a note before adding another.",
            "Hai già {0:N0} ricordi salvati. Elimina una nota prima di aggiungerne un’altra.",
            "Vous avez déjà {0:N0} souvenirs enregistrés. Supprimez une note avant d’en ajouter une autre.",
            "Du hast bereits {0:N0} gespeicherte Erinnerungen. Lösche eine Notiz, bevor du eine weitere hinzufügst.",
            "Ya tienes {0:N0} recuerdos guardados. Borra una nota antes de añadir otra.",
            "Bạn đã có {0:N0} ghi nhớ. Xóa một ghi chú trước khi thêm ghi chú khác."));
        entries.Add("Memory.None", new(
            "No saved memories yet.",
            "Non ci sono ancora ricordi salvati.",
            "Aucun souvenir enregistré pour le moment.",
            "Noch keine gespeicherten Erinnerungen.",
            "Todavía no hay recuerdos guardados.",
            "Chưa có ghi nhớ đã lưu."));
        entries.Add("Memory.NotFound", new(
            "No matching saved memory.",
            "Nessun ricordo salvato corrispondente.",
            "Aucun souvenir enregistré correspondant.",
            "Keine passende gespeicherte Erinnerung.",
            "No hay un recuerdo guardado que coincida.",
            "Không tìm thấy ghi nhớ phù hợp."));
        entries.Add("Memory.Note", new(
            English: "Note",
            Italian: "Nota",
            French: "Note",
            German: "Notiz",
            Spanish: "Nota",
            Vietnamese: "Ghi chú"));
        entries.Add("Memory.Saved", new(
            "Memory saved: {0}",
            "Ricordo salvato: {0}",
            "Souvenir enregistré : {0}",
            "Erinnerung gespeichert: {0}",
            "Recuerdo guardado: {0}",
            "Đã lưu ghi nhớ: {0}"));
        entries.Add("Memory.Secret", new(
            English: "Memory was not saved because it contains recognizable credentials or redaction markers.",
            Italian: "Memoria non salvata: contiene credenziali riconoscibili o marcatori di redazione.",
            French: "Mémoire non enregistrée : elle contient des identifiants reconnaissables ou des marqueurs de masquage.",
            German: "Erinnerung nicht gespeichert: Sie enthält erkennbare Zugangsdaten oder Schwärzungsmarker.",
            Spanish: "Memoria no guardada: contiene credenciales reconocibles o marcas de ocultación.",
            Vietnamese: "Không lưu ghi nhớ vì chứa thông tin xác thực nhận diện được hoặc dấu đã lọc."));
        entries.Add("Memory.Syntax", new(
            "Use /remember <text>, /memories, or /forget <id>.",
            "Usa /remember <testo>, /memories oppure /forget <id>.",
            "Utilisez /remember <texte>, /memories ou /forget <id>.",
            "Verwenden Sie /remember <Text>, /memories oder /forget <id>.",
            "Usa /remember <texto>, /memories o /forget <id>.",
            "Dùng /remember <văn-bản>, /memories hoặc /forget <id>."));
        entries.Add("Memory.Title", new(
            "Saved memories",
            "Ricordi salvati",
            "Souvenirs enregistrés",
            "Gespeicherte Erinnerungen",
            "Recuerdos guardados",
            "Ghi nhớ đã lưu"));
        entries.Add("Memory.TooLong", new(
            English: "Memory exceeds {0:N0} characters. Keep the note short.",
            Italian: "La memoria supera {0:N0} caratteri. Mantieni la nota breve.",
            French: "La mémoire dépasse {0:N0} caractères. Raccourcissez la note.",
            German: "Die Erinnerung überschreitet {0:N0} Zeichen. Kürzen Sie die Notiz.",
            Spanish: "La memoria supera {0:N0} caracteres. Acorta la nota.",
            Vietnamese: "Ghi nhớ vượt quá {0:N0} ký tự. Hãy rút gọn ghi chú."));
        entries.Add("Query.Empty", new(
            English: "The prompt cannot be empty.",
            Italian: "Il prompt non può essere vuoto.",
            French: "Le prompt ne peut pas être vide.",
            German: "Der Prompt darf nicht leer sein.",
            Spanish: "El prompt no puede estar vacío.",
            Vietnamese: "Prompt không được để trống."));
        entries.Add("Query.Prompt", new(
            English: "Enter your prompt",
            Italian: "Inserisci il prompt",
            French: "Entrez votre prompt",
            German: "Prompt eingeben",
            Spanish: "Escribe tu prompt",
            Vietnamese: "Nhập prompt"));
        entries.Add("Query.Response", new(
            English: "Model response",
            Italian: "Risposta del modello",
            French: "Réponse du modèle",
            German: "Modellantwort",
            Spanish: "Respuesta del modelo",
            Vietnamese: "Phản hồi của mô hình"));
        entries.Add("Test.Expected", new(
            English: "Expected a short confirmation from the configured model.",
            Italian: "È attesa una breve conferma dal modello configurato.",
            French: "Une courte confirmation du modèle configuré est attendue.",
            German: "Eine kurze Bestätigung des konfigurierten Modells wird erwartet.",
            Spanish: "Se espera una confirmación breve del modelo configurado.",
            Vietnamese: "Mô hình đã cấu hình phải trả về một xác nhận ngắn."));
        entries.Add("Test.Success", new(
            English: "Connection confirmed in {0} ms.",
            Italian: "Connessione confermata in {0} ms.",
            French: "Connexion confirmée en {0} ms.",
            German: "Verbindung in {0} ms bestätigt.",
            Spanish: "Conexión confirmada en {0} ms.",
            Vietnamese: "Kết nối thành công trong {0} ms."));
        entries.Add("Test.Title", new(
            English: "AI connection test",
            Italian: "Test connessione AI",
            French: "Test de connexion IA",
            German: "KI-Verbindungstest",
            Spanish: "Prueba de conexión de IA",
            Vietnamese: "Kiểm tra kết nối AI"));
    }
}
