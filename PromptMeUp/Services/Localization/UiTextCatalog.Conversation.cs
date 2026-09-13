// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language conversation UI catalog.</summary>
    private static void AddConversationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Chat.Assistant", new(
            English: "PromptMeUp",
            Italian: "PromptMeUp",
            French: "PromptMeUp",
            German: "PromptMeUp",
            Spanish: "PromptMeUp",
            Vietnamese: "PromptMeUp"));
        entries.Add("Chat.Branding", new(
            English: "A focused workspace for safe, terminal-native assistance.",
            Italian: "Uno spazio di lavoro mirato per assistenza sicura e nativa da terminale.",
            French: "Un espace ciblé pour une assistance sûre, native du terminal.",
            German: "Ein fokussierter Bereich für sichere, terminalnative Unterstützung.",
            Spanish: "Un espacio enfocado para ayuda segura y nativa de terminal.",
            Vietnamese: "Không gian tập trung cho trợ giúp an toàn, thuần terminal."));
        entries.Add("Chat.Cleared", new(
            English: "Conversation cleared.",
            Italian: "Conversazione azzerata.",
            French: "Conversation effacée.",
            German: "Gespräch gelöscht.",
            Spanish: "Conversación borrada.",
            Vietnamese: "Đã xóa hội thoại."));
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
            English: "Delete a saved memory",
            Italian: "Elimina una memoria salvata",
            French: "Supprimer une mémoire enregistrée",
            German: "Gespeicherte Erinnerung löschen",
            Spanish: "Eliminar una memoria guardada",
            Vietnamese: "Xóa ghi nhớ đã lưu"));
        entries.Add("Chat.Command.Memories", new(
            English: "List global and current-project memories",
            Italian: "Elenca memorie globali e del progetto corrente",
            French: "Lister les mémoires globales et du projet courant",
            German: "Globale und aktuelle Projekterinnerungen auflisten",
            Spanish: "Listar memorias globales y del proyecto actual",
            Vietnamese: "Liệt kê ghi nhớ chung và của dự án hiện tại"));
        entries.Add("Chat.Command.Remember", new(
            English: "Save a note (project by default)",
            Italian: "Salva una nota (progetto predefinito)",
            French: "Enregistrer une note (projet par défaut)",
            German: "Notiz speichern (standardmäßig im Projekt)",
            Spanish: "Guardar una nota (proyecto por defecto)",
            Vietnamese: "Lưu ghi chú (mặc định cho dự án)"));
        entries.Add("Chat.Command.RememberSyntax", new(
            English: "/remember [global|project] <text>",
            Italian: "/remember [global|project] <testo>",
            French: "/remember [global|project] <texte>",
            German: "/remember [global|project] <Text>",
            Spanish: "/remember [global|project] <texto>",
            Vietnamese: "/remember [global|project] <văn-bản>"));
        entries.Add("Chat.Command.Run", new(
            English: "Preview a shell command, assess its risk, then request exact authorization.",
            Italian: "Mostra l'anteprima di un comando, ne valuta il rischio e chiede l'autorizzazione esatta.",
            French: "Prévisualise une commande, évalue son risque puis demande une autorisation exacte.",
            German: "Zeigt eine Befehlsvorschau, bewertet das Risiko und fragt nach exakter Freigabe.",
            Spanish: "Previsualiza un comando, evalúa su riesgo y solicita autorización exacta.",
            Vietnamese: "Xem trước lệnh, đánh giá rủi ro rồi yêu cầu cấp quyền chính xác."));
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
            English: "Memory deleted.",
            Italian: "Memoria eliminata.",
            French: "Mémoire supprimée.",
            German: "Erinnerung gelöscht.",
            Spanish: "Memoria eliminada.",
            Vietnamese: "Đã xóa ghi nhớ."));
        entries.Add("Memory.Global", new(
            English: "Global",
            Italian: "Globale",
            French: "Globale",
            German: "Global",
            Spanish: "Global",
            Vietnamese: "Chung"));
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
            English: "This scope already contains {0:N0} memories. Delete a note before saving another.",
            Italian: "Questo ambito contiene già {0:N0} memorie. Elimina una nota prima di salvarne un'altra.",
            French: "Cette portée contient déjà {0:N0} mémoires. Supprimez une note avant d'en enregistrer une autre.",
            German: "Dieser Geltungsbereich enthält bereits {0:N0} Erinnerungen. Löschen Sie vor dem Speichern eine Notiz.",
            Spanish: "Este ámbito ya contiene {0:N0} memorias. Elimina una nota antes de guardar otra.",
            Vietnamese: "Phạm vi này đã có {0:N0} ghi nhớ. Xóa một ghi chú trước khi lưu thêm."));
        entries.Add("Memory.None", new(
            English: "No global or current-project memories saved yet.",
            Italian: "Nessuna memoria globale o del progetto corrente salvata.",
            French: "Aucune mémoire globale ou du projet courant enregistrée.",
            German: "Noch keine globalen oder aktuellen Projekterinnerungen gespeichert.",
            Spanish: "Todavía no hay memorias globales ni del proyecto actual.",
            Vietnamese: "Chưa có ghi nhớ chung hoặc của dự án hiện tại."));
        entries.Add("Memory.NotFound", new(
            English: "No matching global or current-project memory.",
            Italian: "Nessuna memoria corrispondente globale o del progetto corrente.",
            French: "Aucune mémoire globale ou du projet courant correspondante.",
            German: "Keine passende globale oder aktuelle Projekterinnerung gefunden.",
            Spanish: "No hay memoria global o del proyecto actual con ese ID.",
            Vietnamese: "Không tìm thấy ghi nhớ chung hoặc của dự án hiện tại tương ứng."));
        entries.Add("Memory.Note", new(
            English: "Note",
            Italian: "Nota",
            French: "Note",
            German: "Notiz",
            Spanish: "Nota",
            Vietnamese: "Ghi chú"));
        entries.Add("Memory.Project", new(
            English: "Current project",
            Italian: "Progetto corrente",
            French: "Projet courant",
            German: "Aktuelles Projekt",
            Spanish: "Proyecto actual",
            Vietnamese: "Dự án hiện tại"));
        entries.Add("Memory.Saved", new(
            English: "Memory saved: {0}",
            Italian: "Memoria salvata: {0}",
            French: "Mémoire enregistrée : {0}",
            German: "Erinnerung gespeichert: {0}",
            Spanish: "Memoria guardada: {0}",
            Vietnamese: "Đã lưu ghi nhớ: {0}"));
        entries.Add("Memory.Scope", new(
            English: "Scope",
            Italian: "Ambito",
            French: "Portée",
            German: "Geltungsbereich",
            Spanish: "Ámbito",
            Vietnamese: "Phạm vi"));
        entries.Add("Memory.Secret", new(
            English: "Memory was not saved because it contains recognizable credentials or redaction markers.",
            Italian: "Memoria non salvata: contiene credenziali riconoscibili o marcatori di redazione.",
            French: "Mémoire non enregistrée : elle contient des identifiants reconnaissables ou des marqueurs de masquage.",
            German: "Erinnerung nicht gespeichert: Sie enthält erkennbare Zugangsdaten oder Schwärzungsmarker.",
            Spanish: "Memoria no guardada: contiene credenciales reconocibles o marcas de ocultación.",
            Vietnamese: "Không lưu ghi nhớ vì chứa thông tin xác thực nhận diện được hoặc dấu đã lọc."));
        entries.Add("Memory.Syntax", new(
            English: "Use /remember [global|project] <text>, /memories, or /forget <id>.",
            Italian: "Usa /remember [global|project] <testo>, /memories oppure /forget <id>.",
            French: "Utilisez /remember [global|project] <texte>, /memories ou /forget <id>.",
            German: "Verwenden Sie /remember [global|project] <Text>, /memories oder /forget <id>.",
            Spanish: "Usa /remember [global|project] <texto>, /memories o /forget <id>.",
            Vietnamese: "Dùng /remember [global|project] <văn-bản>, /memories hoặc /forget <id>."));
        entries.Add("Memory.Title", new(
            English: "Saved memories",
            Italian: "Memorie salvate",
            French: "Mémoires enregistrées",
            German: "Gespeicherte Erinnerungen",
            Spanish: "Memorias guardadas",
            Vietnamese: "Ghi nhớ đã lưu"));
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
