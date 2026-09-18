// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds all six translations for direct saved-memory commands and their verified outcomes.</summary>
    private static void AddMemoryCliEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("MemoryCli.Usage", new(
            "Use hm --remember <text> or hm --forget <id or description>.",
            "Usa hm --remember <testo> oppure hm --forget <id o descrizione>.",
            "Utilisez hm --remember <texte> ou hm --forget <id ou description>.",
            "Verwenden Sie hm --remember <Text> oder hm --forget <ID oder Beschreibung>.",
            "Usa hm --remember <texto> o hm --forget <id o descripción>.",
            "Dùng hm --remember <nội dung> hoặc hm --forget <ID hoặc mô tả>."));
        entries.Add("MemoryCli.RememberHelp", new(
            "Save a note for future conversations",
            "Salva una nota per le prossime conversazioni",
            "Enregistrer une note pour les prochaines conversations",
            "Notiz für künftige Gespräche speichern",
            "Guardar una nota para futuras conversaciones",
            "Lưu ghi chú cho các cuộc trò chuyện sau"));
        entries.Add("MemoryCli.ForgetHelp", new(
            English: "Choose and delete a saved note; AI is used only to search by description",
            Italian: "Scegli ed elimina un ricordo; l’AI serve solo per cercarlo con una descrizione",
            French: "Choisir et supprimer un souvenir ; IA seulement pour chercher par description",
            German: "Erinnerung auswählen und löschen; KI nur für die Suche per Beschreibung",
            Spanish: "Elegir y borrar un recuerdo; IA solo para buscarlo por descripción",
            Vietnamese: "Chọn và xóa ghi nhớ; chỉ dùng AI khi tìm theo mô tả"));
        entries.Add("MemoryCli.Choose", new(
            "Choose the memory to forget",
            "Scegli il ricordo da dimenticare",
            "Choisissez le souvenir à oublier",
            "Erinnerung zum Vergessen auswählen",
            "Elige el recuerdo que quieres olvidar",
            "Chọn ghi nhớ muốn quên"));
        entries.Add("MemoryCli.Page", new(
            English: "Page {0} of {1}", Italian: "Pagina {0} di {1}", French: "Page {0} sur {1}",
            German: "Seite {0} von {1}", Spanish: "Página {0} de {1}", Vietnamese: "Trang {0} trên {1}"));
        entries.Add("MemoryCli.Next", new(
            English: "Next page (returns to the first after the last)", Italian: "Pagina successiva (dopo l'ultima torna alla prima)",
            French: "Page suivante (revient à la première après la dernière)", German: "Nächste Seite (nach der letzten wieder die erste)",
            Spanish: "Página siguiente (tras la última vuelve a la primera)", Vietnamese: "Trang tiếp (sau trang cuối trở về trang đầu)"));
        entries.Add("MemoryCli.DigitHint", new(
            English: "Press a displayed number; no Enter needed. 0 cancels.",
            Italian: "Premi uno dei numeri mostrati; non serve Invio. 0 annulla.",
            French: "Appuyez sur un numéro affiché ; Entrée inutile. 0 annule.",
            German: "Drücken Sie eine angezeigte Ziffer; kein Enter nötig. 0 bricht ab.",
            Spanish: "Pulsa un número mostrado; no hace falta Intro. 0 cancela.",
            Vietnamese: "Nhấn một số đang hiển thị; không cần Enter. 0 để hủy."));
        entries.Add("MemoryCli.Changed", new(
            English: "The note changed or was removed during lookup. Nothing was deleted; review the memories and retry.",
            Italian: "La nota è cambiata o è stata rimossa durante la ricerca. Non è stato eliminato nulla; controlla le memorie e riprova.",
            French: "La note a changé ou a été retirée pendant la recherche. Rien n'a été supprimé ; vérifiez les notes et réessayez.",
            German: "Die Notiz wurde während der Suche geändert oder entfernt. Nichts wurde gelöscht; prüfen Sie die Notizen und versuchen Sie es erneut.",
            Spanish: "La nota cambió o se quitó durante la búsqueda. No se eliminó nada; revisa las notas y vuelve a intentarlo.",
            Vietnamese: "Ghi chú đã thay đổi hoặc bị xóa trong lúc tìm. Chưa xóa gì; kiểm tra ghi chú rồi thử lại."));
        entries.Add("MemoryCli.Forgotten", new(
            "Memory {0} deleted.",
            "Ricordo {0} eliminato.",
            "Souvenir {0} supprimé.",
            "Erinnerung {0} gelöscht.",
            "Recuerdo {0} eliminado.",
            "Đã xóa ghi nhớ {0}."));
        entries.Add("MemoryCli.InvalidResponse", new(
            English: "AI memory lookup returned an invalid selection. Nothing was deleted.",
            Italian: "La ricerca AI delle memorie ha restituito una selezione non valida. Non è stato eliminato nulla.",
            French: "La recherche IA a renvoyé une sélection non valide. Rien n'a été supprimé.",
            German: "Die KI-Suche lieferte eine ungültige Auswahl. Nichts wurde gelöscht.",
            Spanish: "La búsqueda con IA devolvió una selección no válida. No se eliminó nada.",
            Vietnamese: "AI tìm ghi chú trả về lựa chọn không hợp lệ. Chưa xóa gì."));
    }
}
