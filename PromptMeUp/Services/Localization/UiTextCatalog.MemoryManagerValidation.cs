// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds localized validation feedback for editing saved memories.</summary>
    private static void AddMemoryManagerValidationEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Memory.Duplicate", new(
            "This note is already saved. Edit the existing memory or change the text.",
            "Questa nota è già salvata. Modifica il ricordo esistente o cambia il testo.",
            "Cette note est déjà enregistrée. Modifiez le souvenir existant ou changez le texte.",
            "Diese Notiz ist bereits gespeichert. Bearbeite die vorhandene Erinnerung oder ändere den Text.",
            "Esta nota ya está guardada. Edita el recuerdo existente o cambia el texto.",
            "Ghi chú này đã được lưu. Sửa ghi nhớ hiện có hoặc đổi nội dung."));
    }
}
