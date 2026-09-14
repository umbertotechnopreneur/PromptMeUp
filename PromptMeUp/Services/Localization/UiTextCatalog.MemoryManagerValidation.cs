// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds localized validation feedback for editing saved memories.</summary>
    private static void AddMemoryManagerValidationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Memory.Duplicate", new(
            English: "Another memory already has this note in the selected scope. Edit that memory or change the note.",
            Italian: "Un'altra memoria contiene già questa nota nell'ambito scelto. Modifica quella memoria o cambia la nota.",
            French: "Une autre mémoire contient déjà cette note dans la portée choisie. Modifiez cette mémoire ou changez la note.",
            German: "Eine andere Erinnerung enthält diese Notiz bereits im gewählten Geltungsbereich. Bearbeiten Sie diese Erinnerung oder ändern Sie die Notiz.",
            Spanish: "Otra memoria ya contiene esta nota en el ámbito elegido. Edita esa memoria o cambia la nota.",
            Vietnamese: "Một ghi nhớ khác đã có nội dung này trong phạm vi đã chọn. Hãy sửa ghi nhớ đó hoặc đổi nội dung."));
    }
}
