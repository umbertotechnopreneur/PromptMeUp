// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language fullscreen form and terminal theme catalog.</summary>
    private static void AddFormEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Form.General", new(
            English: "General",
            Italian: "Generali",
            French: "Général",
            German: "Allgemein",
            Spanish: "General",
            Vietnamese: "Chung"));
        entries.Add("Form.Advanced", new(
            English: "Advanced",
            Italian: "Avanzate",
            French: "Avancé",
            German: "Erweitert",
            Spanish: "Avanzado",
            Vietnamese: "Nâng cao"));
        entries.Add("Theme.Title", new(
            English: "Theme",
            Italian: "Tema",
            French: "Thème",
            German: "Design",
            Spanish: "Tema",
            Vietnamese: "Giao diện"));
        entries.Add("Theme.Select", new(
            English: "Color theme",
            Italian: "Tema colori",
            French: "Thème de couleurs",
            German: "Farbschema",
            Spanish: "Tema de colores",
            Vietnamese: "Bảng màu"));
        entries.Add("Theme.Preview", new(
            English: "Preview the selected colors before saving.",
            Italian: "Visualizza i colori selezionati prima di salvare.",
            French: "Prévisualisez les couleurs sélectionnées avant d'enregistrer.",
            German: "Sehen Sie sich die ausgewählten Farben vor dem Speichern an.",
            Spanish: "Previsualiza los colores seleccionados antes de guardar.",
            Vietnamese: "Xem trước các màu đã chọn trước khi lưu."));
        entries.Add("Form.CredentialsHelp", new(
            English: "Leave a replacement blank to keep the configured key. Secret input is never displayed.",
            Italian: "Lascia vuota la sostituzione per mantenere la chiave configurata. L'immissione segreta non viene mai mostrata.",
            French: "Laissez le remplacement vide pour conserver la clé configurée. La saisie secrète n'est jamais affichée.",
            German: "Lassen Sie den Ersatz leer, um den konfigurierten Schlüssel zu behalten. Geheime Eingaben werden nie angezeigt.",
            Spanish: "Deja el reemplazo vacío para mantener la clave configurada. La entrada secreta nunca se muestra.",
            Vietnamese: "Để trống phần thay thế để giữ khóa đã cấu hình. Nội dung bí mật đã nhập không bao giờ được hiển thị."));
        entries.Add("Form.ApiKeyReplacement", new(
            English: "API key replacement",
            Italian: "Sostituzione chiave API",
            French: "Remplacement de la clé API",
            German: "API-Schlüssel ersetzen",
            Spanish: "Reemplazo de la clave API",
            Vietnamese: "Thay khóa API"));
        entries.Add("Form.AdminKeyReplacement", new(
            English: "Admin key replacement",
            Italian: "Sostituzione chiave amministratore",
            French: "Remplacement de la clé administrateur",
            German: "Administratorschlüssel ersetzen",
            Spanish: "Reemplazo de la clave de administrador",
            Vietnamese: "Thay khóa quản trị"));
        entries.Add("Form.TestConnection", new(
            English: "Test connection after saving",
            Italian: "Verifica la connessione dopo il salvataggio",
            French: "Tester la connexion après l'enregistrement",
            German: "Verbindung nach dem Speichern prüfen",
            Spanish: "Probar la conexión después de guardar",
            Vietnamese: "Kiểm tra kết nối sau khi lưu"));
        entries.Add("Form.Back", new(
            English: "Back",
            Italian: "Indietro",
            French: "Retour",
            German: "Zurück",
            Spanish: "Atrás",
            Vietnamese: "Quay lại"));
        entries.Add("Form.Next", new(
            English: "Next",
            Italian: "Avanti",
            French: "Suivant",
            German: "Weiter",
            Spanish: "Siguiente",
            Vietnamese: "Tiếp"));
        entries.Add("Form.Review", new(
            English: "Review",
            Italian: "Riepilogo",
            French: "Vérifier",
            German: "Prüfen",
            Spanish: "Revisar",
            Vietnamese: "Xem lại"));
        entries.Add("Form.Save", new(
            English: "Save",
            Italian: "Salva",
            French: "Enregistrer",
            German: "Speichern",
            Spanish: "Guardar",
            Vietnamese: "Lưu"));
        entries.Add("Form.Cancel", new(
            English: "Cancel",
            Italian: "Annulla",
            French: "Annuler",
            German: "Abbrechen",
            Spanish: "Cancelar",
            Vietnamese: "Hủy"));
        entries.Add("Form.Sections", new(
            English: "Sections",
            Italian: "Sezioni",
            French: "Sections",
            German: "Abschnitte",
            Spanish: "Secciones",
            Vietnamese: "Các mục"));
        entries.Add("Form.SectionsHelp", new(
            English: "Choose a section with Up/Down, then press Enter to edit. F6 switches between sections and fields. Changes stay in the draft until you choose Save.",
            Italian: "Scegli una sezione con Su/Giù, poi premi Invio per modificare. F6 passa tra sezioni e campi. Le modifiche restano nella bozza finché scegli Salva.",
            French: "Choisissez une section avec Haut/Bas, puis appuyez sur Entrée pour modifier. F6 alterne entre sections et champs. Les modifications restent en brouillon jusqu'à Enregistrer.",
            German: "Wählen Sie mit Auf/Ab einen Abschnitt und drücken Sie Enter zum Bearbeiten. F6 wechselt zwischen Abschnitten und Feldern. Änderungen bleiben bis zum Speichern im Entwurf.",
            Spanish: "Elige una sección con Arriba/Abajo y pulsa Intro para editar. F6 alterna entre secciones y campos. Los cambios quedan en el borrador hasta elegir Guardar.",
            Vietnamese: "Chọn mục bằng Lên/Xuống, rồi nhấn Enter để sửa. F6 chuyển giữa mục và trường. Thay đổi được giữ trong bản nháp đến khi bạn chọn Lưu."));
        entries.Add("Form.SectionsFooter", new(
            English: "Up/Down: sections | Enter/Right/Tab: fields | F6: switch focus | Esc: cancel",
            Italian: "Su/Giù: sezioni | Invio/Destra/Tab: campi | F6: cambia area | Esc: annulla",
            French: "Haut/Bas : sections | Entrée/Droite/Tab : champs | F6 : changer de zone | Esc : annuler",
            German: "Auf/Ab: Abschnitte | Enter/Rechts/Tab: Felder | F6: Fokus wechseln | Esc: abbrechen",
            Spanish: "Arriba/Abajo: secciones | Intro/Derecha/Tab: campos | F6: cambiar foco | Esc: cancelar",
            Vietnamese: "Lên/Xuống: mục | Enter/Phải/Tab: trường | F6: chuyển vùng | Esc: hủy"));
        entries.Add("Form.SectionsFooterCompact", new(
            English: "Up/Down: sections | Enter: fields | Esc: cancel",
            Italian: "Su/Giù: sezioni | Invio: campi | Esc: annulla",
            French: "Haut/Bas: sections | Entrée: champs | Esc: annuler",
            German: "Auf/Ab: Abschnitte | Enter: Felder | Esc: Abbruch",
            Spanish: "Arriba/Abajo: secciones | Intro: campos | Esc: cancelar",
            Vietnamese: "Lên/Xuống: mục | Enter: trường | Esc: hủy"));
        entries.Add("Form.NavigationFooter", new(
            English: "Tab: fields/actions | F6: sections | Left/Right: buttons | Enter: choose | Esc: cancel",
            Italian: "Tab: campi/azioni | F6: sezioni | Sinistra/Destra: pulsanti | Invio: scegli | Esc: annulla",
            French: "Tab : champs/actions | F6 : sections | Gauche/Droite : boutons | Entrée : choisir | Esc : annuler",
            German: "Tab: Felder/Aktionen | F6: Abschnitte | Links/Rechts: Schaltflächen | Enter: wählen | Esc: abbrechen",
            Spanish: "Tab: campos/acciones | F6: secciones | Izquierda/Derecha: botones | Intro: elegir | Esc: cancelar",
            Vietnamese: "Tab: trường/thao tác | F6: mục | Trái/Phải: nút | Enter: chọn | Esc: hủy"));
        entries.Add("Form.NavigationFooterCompact", new(
            English: "F6: sections | Enter: edit | Esc: cancel",
            Italian: "F6: sezioni | Invio: modifica | Esc: annulla",
            French: "F6: sections | Entrée: modifier | Esc: annuler",
            German: "F6: Abschnitte | Enter: ändern | Esc: Abbruch",
            Spanish: "F6: secciones | Intro: editar | Esc: cancelar",
            Vietnamese: "F6: mục | Enter: sửa | Esc: hủy"));
        entries.Add("Form.NavigationHelp", new(
            English: "F6 switches between the sidebar and fields. Tab reaches Save and Cancel; Left/Right moves between the buttons. Enter chooses the focused action.",
            Italian: "F6 passa tra barra laterale e campi. Tab raggiunge Salva e Annulla; Sinistra/Destra passa tra i pulsanti. Invio sceglie l'azione selezionata.",
            French: "F6 alterne entre la barre latérale et les champs. Tab rejoint Enregistrer et Annuler ; Gauche/Droite passe entre les boutons. Entrée choisit l'action active.",
            German: "F6 wechselt zwischen Seitenleiste und Feldern. Tab erreicht Speichern und Abbrechen; Links/Rechts wechselt zwischen den Schaltflächen. Enter wählt die aktive Aktion.",
            Spanish: "F6 alterna entre la barra lateral y los campos. Tab llega a Guardar y Cancelar; Izquierda/Derecha cambia de botón. Intro elige la acción enfocada.",
            Vietnamese: "F6 chuyển giữa thanh bên và trường. Tab đến Lưu và Hủy; Trái/Phải chuyển giữa các nút. Enter chọn thao tác đang được trỏ đến."));
        entries.Add("Form.Footer", new(
            English: "Tab/arrows: fields | PgUp/PgDn: sections | Left/Right: buttons | Enter: choose | Esc: cancel",
            Italian: "Tab/frecce: campi | PgUp/PgDn: sezioni | Sinistra/Destra: pulsanti | Invio: scegli | Esc: annulla",
            French: "Tab/flèches : champs | PgUp/PgDn : sections | Gauche/Droite : boutons | Entrée : choisir | Esc : annuler",
            German: "Tab/Pfeile: Felder | PgUp/PgDn: Abschnitte | Links/Rechts: Schaltflächen | Enter: wählen | Esc: abbrechen",
            Spanish: "Tab/flechas: campos | PgUp/PgDn: secciones | Izquierda/Derecha: botones | Intro: elegir | Esc: cancelar",
            Vietnamese: "Tab/mũi tên: trường | PgUp/PgDn: mục | Trái/Phải: nút | Enter: chọn | Esc: hủy"));
        entries.Add("Form.EditFooter", new(
            English: "Enter: accept | Ctrl+U: clear | Esc: cancel",
            Italian: "Invio: conferma | Ctrl+U: svuota | Esc: annulla",
            French: "Entrée : accepter | Ctrl+U : vider | Esc : annuler",
            German: "Enter: übernehmen | Strg+U: leeren | Esc: abbrechen",
            Spanish: "Intro: aceptar | Ctrl+U: vaciar | Esc: cancelar",
            Vietnamese: "Enter: chấp nhận | Ctrl+U: xóa | Esc: hủy"));
        entries.Add("Form.FooterCompact", new(
            English: "Tab: next | Enter: choose | Esc: cancel",
            Italian: "Tab: avanti | Invio: scegli | Esc: annulla",
            French: "Tab: suite | Entrée: choisir | Esc: annuler",
            German: "Tab: weiter | Enter: wählen | Esc: Abbruch",
            Spanish: "Tab: siguiente | Intro: elegir | Esc: cancelar",
            Vietnamese: "Tab: tiếp | Enter: chọn | Esc: hủy"));
        entries.Add("Form.EditFooterCompact", new(
            English: "Esc: cancel | Enter: accept | Ctrl+U: clear",
            Italian: "Esc: annulla | Invio: conferma | Ctrl+U: svuota",
            French: "Esc: annuler | Entrée: valider | Ctrl+U: vider",
            German: "Esc: Abbruch | Enter: übernehmen | Strg+U: leeren",
            Spanish: "Esc: cancelar | Intro: aceptar | Ctrl+U: vaciar",
            Vietnamese: "Esc: hủy | Enter: chấp nhận | Ctrl+U: xóa"));
        entries.Add("Form.ReviewFooter", new(
            English: "Tab/arrows: actions | PgUp/PgDn: scroll | Enter: choose | Esc: cancel",
            Italian: "Tab/frecce: azioni | PgUp/PgDn: scorri | Invio: scegli | Esc: annulla",
            French: "Tab/flèches : actions | PgUp/PgDn : défiler | Entrée : choisir | Esc : annuler",
            German: "Tab/Pfeile: Aktionen | PgUp/PgDn: blättern | Enter: wählen | Esc: abbrechen",
            Spanish: "Tab/flechas: acciones | PgUp/PgDn: desplazar | Intro: elegir | Esc: cancelar",
            Vietnamese: "Tab/mũi tên: thao tác | PgUp/PgDn: cuộn | Enter: chọn | Esc: hủy"));
        entries.Add("Form.ReviewFooterCompact", new(
            English: "Esc: cancel | Enter: choose | Tab: action | PgUp/PgDn",
            Italian: "Esc: annulla | Invio: scegli | Tab: azione | PgUp/PgDn",
            French: "Esc: annuler | Entrée: choisir | Tab: action | PgUp/PgDn",
            German: "Esc: Abbruch | Enter: wählen | Tab: Aktion | PgUp/PgDn",
            Spanish: "Esc: cancelar | Intro: elegir | Tab: acción | PgUp/PgDn",
            Vietnamese: "Esc: hủy | Enter: chọn | Tab: thao tác | PgUp/PgDn"));
        entries.Add("Form.Help", new(
            English: "Change fields across sections, then choose Save or Cancel.",
            Italian: "Modifica i campi nelle sezioni, poi scegli Salva o Annulla.",
            French: "Modifiez les champs des sections, puis choisissez Enregistrer ou Annuler.",
            German: "Ändern Sie Felder in den Abschnitten und wählen Sie Speichern oder Abbrechen.",
            Spanish: "Cambia los campos de las secciones y elige Guardar o Cancelar.",
            Vietnamese: "Sửa các trường trong từng mục, rồi chọn Lưu hoặc Hủy."));
        entries.Add("Form.PreambleHelp", new(
            English: "Optional instructions, up to 500 words. Do not include secrets or instructions that override safety rules.",
            Italian: "Istruzioni facoltative, fino a 500 parole. Non includere segreti o istruzioni che scavalcano le regole di sicurezza.",
            French: "Instructions facultatives, jusqu'à 500 mots. N'incluez aucun secret ni instruction contournant les règles de sécurité.",
            German: "Optionale Anweisungen mit bis zu 500 Wörtern. Keine Geheimnisse oder Anweisungen zum Umgehen der Sicherheitsregeln eingeben.",
            Spanish: "Instrucciones opcionales, hasta 500 palabras. No incluyas secretos ni instrucciones que eludan las reglas de seguridad.",
            Vietnamese: "Hướng dẫn tùy chọn, tối đa 500 từ. Không đưa vào bí mật hoặc hướng dẫn bỏ qua các quy tắc an toàn."));
        entries.Add("Form.Editing", new(
            English: "Editing",
            Italian: "Modifica",
            French: "Modification",
            German: "Bearbeiten",
            Spanish: "Edición",
            Vietnamese: "Đang sửa"));
        entries.Add("Form.SecretUnchanged", new(
            English: "Unchanged",
            Italian: "Invariata",
            French: "Inchangée",
            German: "Unverändert",
            Spanish: "Sin cambios",
            Vietnamese: "Không thay đổi"));
        entries.Add("Form.SecretEntered", new(
            English: "Replacement entered",
            Italian: "Sostituzione inserita",
            French: "Remplacement saisi",
            German: "Ersatz eingegeben",
            Spanish: "Reemplazo introducido",
            Vietnamese: "Đã nhập khóa thay thế"));
        entries.Add("Form.SecretInput", new(
            English: "Input hidden",
            Italian: "Immissione nascosta",
            French: "Saisie masquée",
            German: "Eingabe verborgen",
            Spanish: "Entrada oculta",
            Vietnamese: "Nội dung nhập được ẩn"));
        entries.Add("Form.Unsaved", new(
            English: "Unsaved changes",
            Italian: "Modifiche non salvate",
            French: "Modifications non enregistrées",
            German: "Ungespeicherte Änderungen",
            Spanish: "Cambios sin guardar",
            Vietnamese: "Thay đổi chưa lưu"));
        entries.Add("Theme.Saved", new(
            English: "Theme saved.",
            Italian: "Tema salvato.",
            French: "Thème enregistré.",
            German: "Design gespeichert.",
            Spanish: "Tema guardado.",
            Vietnamese: "Đã lưu giao diện."));
        entries.Add("Theme.Help", new(
            English: "Open Settings with Theme selected and preview its colors",
            Italian: "Apre Impostazioni con Tema selezionato e mostra l'anteprima dei colori",
            French: "Ouvre les réglages sur la section Thème et prévisualise ses couleurs",
            German: "Öffnet die Einstellungen mit dem Abschnitt Design und einer Farbvorschau",
            Spanish: "Abre Configuración con Tema seleccionado y previsualiza sus colores",
            Vietnamese: "Mở Cài đặt, chọn mục Giao diện và xem trước các màu"));
        entries.Add("Theme.Cyan", new(
            English: "Cyan",
            Italian: "Ciano",
            French: "Cyan",
            German: "Cyan",
            Spanish: "Cian",
            Vietnamese: "Xanh lơ"));
        entries.Add("Theme.Green", new(
            English: "AS/400 Green",
            Italian: "Verde AS/400",
            French: "Vert AS/400",
            German: "AS/400-Grün",
            Spanish: "Verde AS/400",
            Vietnamese: "Xanh lá AS/400"));
        entries.Add("Theme.Amber", new(
            English: "Amber",
            Italian: "Ambra",
            French: "Ambre",
            German: "Bernstein",
            Spanish: "Ámbar",
            Vietnamese: "Hổ phách"));
        entries.Add("Form.Unavailable", new(
            English: "Fullscreen is unavailable in this terminal; settings open in a section menu.",
            Italian: "La modalità a schermo intero non è disponibile in questo terminale; le impostazioni si aprono in un menu a sezioni.",
            French: "Le plein écran est indisponible dans ce terminal ; les réglages s'ouvrent dans un menu de sections.",
            German: "Vollbild ist in diesem Terminal nicht verfügbar; die Einstellungen öffnen sich in einem Abschnittsmenü.",
            Spanish: "La pantalla completa no está disponible en este terminal; la configuración se abre en un menú de secciones.",
            Vietnamese: "Terminal này không hỗ trợ toàn màn hình; cài đặt sẽ mở trong menu các mục."));
        entries.Add("Form.TooSmall", new(
            English: "Enlarge the terminal to at least 60 columns and 20 rows, or press Escape to cancel.",
            Italian: "Allarga il terminale ad almeno 60 colonne e 20 righe, oppure premi Escape per annullare.",
            French: "Agrandissez le terminal à au moins 60 colonnes et 20 lignes, ou appuyez sur Échap pour annuler.",
            German: "Vergrößern Sie das Terminal auf mindestens 60 Spalten und 20 Zeilen oder drücken Sie Escape zum Abbrechen.",
            Spanish: "Amplía el terminal a un mínimo de 60 columnas y 20 filas, o pulsa Escape para cancelar.",
            Vietnamese: "Mở rộng terminal đến ít nhất 60 cột và 20 hàng, hoặc nhấn Escape để hủy."));
        entries.Add("Form.InputTooLong", new(
            English: "Maximum {0} characters.",
            Italian: "Massimo {0} caratteri.",
            French: "{0} caractères maximum.",
            German: "Höchstens {0} Zeichen.",
            Spanish: "Máximo {0} caracteres.",
            Vietnamese: "Tối đa {0} ký tự."));
        entries.Add("Form.EndOfInput", new(
            English: "Interactive input ended before the form was completed.",
            Italian: "L'input interattivo è terminato prima che il modulo fosse completato.",
            French: "La saisie interactive s'est terminée avant que le formulaire soit rempli.",
            German: "Die interaktive Eingabe wurde beendet, bevor das Formular ausgefüllt war.",
            Spanish: "La entrada interactiva terminó antes de completar el formulario.",
            Vietnamese: "Dữ liệu nhập tương tác đã kết thúc trước khi biểu mẫu được hoàn tất."));
    }
}
