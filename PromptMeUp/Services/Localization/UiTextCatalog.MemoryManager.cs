// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds all six translations for the dedicated memory workspace and its passive editing flows.</summary>
    private static void AddMemoryManagerEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Settings.Memories", new(
            English: "Saved memories",
            Italian: "Ricordi salvati",
            French: "Souvenirs enregistrés",
            German: "Gespeicherte Erinnerungen",
            Spanish: "Recuerdos guardados",
            Vietnamese: "Ghi nhớ đã lưu"));
        entries.Add("Help.Memories", new(
            "Read, add, edit, or delete saved memories",
            "Leggi, aggiungi, modifica o elimina i ricordi salvati",
            "Lire, ajouter, modifier ou supprimer les souvenirs enregistrés",
            "Gespeicherte Erinnerungen lesen, hinzufügen, ändern oder löschen",
            "Leer, añadir, editar o borrar recuerdos guardados",
            "Đọc, thêm, sửa hoặc xóa ghi nhớ đã lưu"));
        entries.Add("MemoryManager.Help", new(
            "Your saved memories, available in every conversation.",
            "I tuoi ricordi salvati, disponibili in ogni conversazione.",
            "Vos souvenirs enregistrés, disponibles dans chaque conversation.",
            "Deine gespeicherten Erinnerungen, in jedem Gespräch verfügbar.",
            "Tus recuerdos guardados, disponibles en cada conversación.",
            "Ghi nhớ đã lưu của bạn, dùng được trong mọi cuộc trò chuyện."));
        entries.Add("MemoryManager.OpenHint", new(
            "Enter/Right: open memories",
            "Invio/Destra: apri ricordi",
            "Entrée/Droite : ouvrir les souvenirs",
            "Enter/Rechts: Erinnerungen öffnen",
            "Intro/Derecha: abrir recuerdos",
            "Enter/Phải: mở ghi nhớ"));
        entries.Add("MemoryManager.Create", new(
            English: "Create",
            Italian: "Crea",
            French: "Créer",
            German: "Erstellen",
            Spanish: "Crear",
            Vietnamese: "Tạo"));
        entries.Add("MemoryManager.Edit", new(
            English: "Edit",
            Italian: "Modifica",
            French: "Modifier",
            German: "Bearbeiten",
            Spanish: "Editar",
            Vietnamese: "Sửa"));
        entries.Add("MemoryManager.Delete", new(
            English: "Delete",
            Italian: "Elimina",
            French: "Supprimer",
            German: "Löschen",
            Spanish: "Eliminar",
            Vietnamese: "Xóa"));
        entries.Add("MemoryManager.Updated", new(
            English: "Updated",
            Italian: "Aggiornata",
            French: "Modifiée",
            German: "Aktualisiert",
            Spanish: "Actualizada",
            Vietnamese: "Cập nhật"));
        entries.Add("MemoryManager.Choose", new(
            English: "Choose a note or action",
            Italian: "Scegli una nota o un'azione",
            French: "Choisissez une note ou une action",
            German: "Notiz oder Aktion wählen",
            Spanish: "Elige una nota o una acción",
            Vietnamese: "Chọn ghi chú hoặc thao tác"));
        entries.Add("MemoryManager.More", new(
            English: "Use Up/Down to see more choices",
            Italian: "Usa Su/Giù per altre voci",
            French: "Utilisez Haut/Bas pour voir les autres choix",
            German: "Mit Auf/Ab weitere Einträge anzeigen",
            Spanish: "Usa Arriba/Abajo para ver más opciones",
            Vietnamese: "Dùng Lên/Xuống để xem thêm lựa chọn"));
        entries.Add("MemoryManager.DeleteConfirm", new(
            "Delete this memory? Select Delete to confirm.",
            "Eliminare questo ricordo? Scegli Elimina per confermare.",
            "Supprimer ce souvenir ? Choisissez Supprimer pour confirmer.",
            "Diese Erinnerung löschen? Zum Bestätigen Löschen wählen.",
            "¿Eliminar este recuerdo? Elige Eliminar para confirmar.",
            "Xóa ghi nhớ này? Chọn Xóa để xác nhận."));
        entries.Add("MemoryManager.NoteHelp", new(
            English: "Up to 1,000 characters. Enter edits this text area; Ctrl+Enter accepts an edit.",
            Italian: "Fino a 1.000 caratteri. Invio modifica quest'area di testo; Ctrl+Invio conferma la modifica.",
            French: "Jusqu'à 1 000 caractères. Entrée modifie cette zone de texte ; Ctrl+Entrée valide la modification.",
            German: "Bis zu 1.000 Zeichen. Enter bearbeitet dieses Textfeld; Strg+Enter übernimmt die Änderung.",
            Spanish: "Hasta 1.000 caracteres. Intro edita esta área de texto; Ctrl+Intro acepta el cambio.",
            Vietnamese: "Tối đa 1.000 ký tự. Enter sửa vùng văn bản; Ctrl+Enter chấp nhận thay đổi."));
        entries.Add("MemoryManager.EditorKeys", new(
            "Type: edit | Enter: new line | Ctrl+Enter: apply | Esc: discard",
            "Scrivi: modifica | Invio: nuova riga | Ctrl+Invio: applica | Esc: scarta",
            "Saisir : modifier | Entrée : nouvelle ligne | Ctrl+Entrée : appliquer | Échap : annuler",
            "Tippen: ändern | Enter: neue Zeile | Strg+Enter: übernehmen | Esc: verwerfen",
            "Escribir: editar | Intro: nueva línea | Ctrl+Intro: aplicar | Esc: descartar",
            "Nhập: sửa | Enter: dòng mới | Ctrl+Enter: áp dụng | Esc: bỏ"));
        entries.Add("MemoryManager.EditorKeysCompact", new(
            "Ctrl+Enter: apply | Esc: discard",
            "Ctrl+Invio: applica | Esc: scarta",
            "Ctrl+Entrée : appliquer | Échap : annuler",
            "Strg+Enter: übernehmen | Esc: verwerfen",
            "Ctrl+Intro: aplicar | Esc: descartar",
            "Ctrl+Enter: áp dụng | Esc: bỏ"));
        entries.Add("MemoryManager.EditHelp", new(
            "Edit the note, then Save. Memories save separately from Settings.",
            "Modifica la nota, poi Salva. I ricordi si salvano separatamente dalle impostazioni.",
            "Modifiez la note, puis Enregistrer. Les souvenirs sont enregistrés séparément des réglages.",
            "Notiz bearbeiten, dann Speichern. Erinnerungen werden getrennt von Einstellungen gespeichert.",
            "Edita la nota y elige Guardar. Los recuerdos se guardan por separado de Configuración.",
            "Sửa ghi chú rồi Lưu. Ghi nhớ được lưu riêng với Cài đặt."));
        entries.Add("MemoryManager.ReplaceHelp", new(
            English: "Enter replacement text on one line, or press Enter to keep the current note, including all line breaks.",
            Italian: "Inserisci il nuovo testo su una riga, oppure premi Invio per conservare la nota attuale con tutti gli a capo.",
            French: "Saisissez le nouveau texte sur une ligne, ou appuyez sur Entrée pour conserver la note actuelle et ses sauts de ligne.",
            German: "Ersatztext in einer Zeile eingeben oder Enter drücken, um die aktuelle Notiz samt Zeilenumbrüchen zu behalten.",
            Spanish: "Escribe el texto nuevo en una línea o pulsa Intro para conservar la nota actual con sus saltos de línea.",
            Vietnamese: "Nhập nội dung thay thế trên một dòng, hoặc nhấn Enter để giữ ghi chú hiện tại cùng mọi dấu xuống dòng."));
        entries.Add("MemoryManager.ListKeys", new(
            English: "Up/Down: notes | Enter/F6: read | Tab: next | Esc: close",
            Italian: "Su/Giù: note | Invio/F6: leggi | Tab: avanti | Esc: chiudi",
            French: "Haut/Bas : notes | Entrée/F6 : lire | Tab : suite | Esc : fermer",
            German: "Auf/Ab: Notizen | Enter/F6: lesen | Tab: weiter | Esc: schließen",
            Spanish: "Arriba/Abajo: notas | Intro/F6: leer | Tab: siguiente | Esc: cerrar",
            Vietnamese: "Lên/Xuống: ghi chú | Enter/F6: đọc | Tab: tiếp | Esc: đóng"));
        entries.Add("MemoryManager.ListKeysCompact", new(
            English: "Up/Down: notes | Tab: next | Esc: close",
            Italian: "Su/Giù: note | Tab: avanti | Esc: chiudi",
            French: "Haut/Bas: notes | Tab: suite | Esc: fermer",
            German: "Auf/Ab: Notizen | Tab: weiter | Esc: zu",
            Spanish: "Arriba/Abajo: notas | Tab: siguiente | Esc: cerrar",
            Vietnamese: "Lên/Xuống: ghi chú | Tab: tiếp | Esc: đóng"));
        entries.Add("MemoryManager.DetailKeys", new(
            English: "Enter: edit | Up/Down/PgUp/PgDn: scroll | Left/Right: suggestion | Tab/F6: area",
            Italian: "Invio: modifica | Su/Giù/PgUp/PgDn: scorri | Sinistra/Destra: proposta | Tab/F6: area",
            French: "Entrée : modifier | Haut/Bas/PgUp/PgDn : défiler | Gauche/Droite : suggestion | Tab/F6 : zone",
            German: "Enter: ändern | Auf/Ab/PgUp/PgDn: blättern | Links/Rechts: Vorschlag | Tab/F6: Bereich",
            Spanish: "Intro: editar | Arriba/Abajo/PgUp/PgDn: desplazar | Izquierda/Derecha: sugerencia | Tab/F6: área",
            Vietnamese: "Enter: sửa | Lên/Xuống/PgUp/PgDn: cuộn | Trái/Phải: đề xuất | Tab/F6: vùng"));
        entries.Add("MemoryManager.DetailKeysCompact", new(
            English: "Enter: edit | PgUp/PgDn: scroll | Tab/F6: area",
            Italian: "Invio: modifica | PgUp/PgDn: scorri | Tab/F6: area",
            French: "Entrée : modifier | PgUp/PgDn : défiler | Tab/F6 : zone",
            German: "Enter: ändern | PgUp/PgDn: blättern | Tab/F6: Bereich",
            Spanish: "Intro: editar | PgUp/PgDn: desplazar | Tab/F6: área",
            Vietnamese: "Enter: sửa | PgUp/PgDn: cuộn | Tab/F6: vùng"));
        entries.Add("MemoryManager.ActionKeys", new(
            English: "Left/Right/Tab: actions | Enter: choose | F6: notes | Esc: cancel",
            Italian: "Sinistra/Destra/Tab: azioni | Invio: scegli | F6: note | Esc: annulla",
            French: "Gauche/Droite/Tab : actions | Entrée : choisir | F6 : notes | Esc : annuler",
            German: "Links/Rechts/Tab: Aktionen | Enter: wählen | F6: Notizen | Esc: abbrechen",
            Spanish: "Izquierda/Derecha/Tab: acciones | Intro: elegir | F6: notas | Esc: cancelar",
            Vietnamese: "Trái/Phải/Tab: thao tác | Enter: chọn | F6: ghi chú | Esc: hủy"));
        entries.Add("MemoryManager.ActionKeysCompact", new(
            English: "Tab: actions | Enter: choose | Esc: cancel",
            Italian: "Tab: azioni | Invio: scegli | Esc: annulla",
            French: "Tab: actions | Entrée: choisir | Esc: annuler",
            German: "Tab: Aktionen | Enter: wählen | Esc: Abbruch",
            Spanish: "Tab: acciones | Intro: elegir | Esc: cancelar",
            Vietnamese: "Tab: thao tác | Enter: chọn | Esc: hủy"));
    }
}
