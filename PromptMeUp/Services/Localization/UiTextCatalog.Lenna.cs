// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language Lenna command catalog.</summary>
    private static void AddLennaEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Lenna.Title", new(
            English: "Lenna",
            Italian: "Lenna",
            French: "Lenna",
            German: "Lenna",
            Spanish: "Lenna",
            Vietnamese: "Lenna"));
        entries.Add("Lenna.Caption", new(
            English: "A familiar face from simpler computing days.",
            Italian: "Un volto familiare dai tempi di un'informatica più semplice.",
            French: "Un visage familier d'une époque informatique plus simple.",
            German: "Ein vertrautes Gesicht aus einfacheren Computerzeiten.",
            Spanish: "Un rostro familiar de tiempos más sencillos de la informática.",
            Vietnamese: "Gương mặt quen thuộc từ những ngày máy tính còn đơn giản hơn."));
        entries.Add("Lenna.LoadError", new(
            English: "The bundled Lenna image could not be read. Reinstall PromptMeUp to restore it.",
            Italian: "Impossibile leggere l'immagine Lenna inclusa. Reinstalla PromptMeUp per ripristinarla.",
            French: "Impossible de lire l'image Lenna incluse. Réinstallez PromptMeUp pour la restaurer.",
            German: "Das mitgelieferte Lenna-Bild konnte nicht gelesen werden. Installieren Sie PromptMeUp erneut, um es wiederherzustellen.",
            Spanish: "No se pudo leer la imagen Lenna incluida. Reinstala PromptMeUp para restaurarla.",
            Vietnamese: "Không thể đọc ảnh Lenna đi kèm. Hãy cài lại PromptMeUp để khôi phục ảnh."));
        entries.Add("Lenna.ColorRequired", new(
            English: "Lenna needs a terminal with ANSI color enabled. Open a color terminal and run hm lenna again.",
            Italian: "Lenna richiede un terminale con colori ANSI attivi. Apri un terminale a colori ed esegui di nuovo hm lenna.",
            French: "Lenna nécessite un terminal avec les couleurs ANSI activées. Ouvrez un terminal couleur et relancez hm lenna.",
            German: "Lenna benötigt ein Terminal mit aktivierten ANSI-Farben. Öffnen Sie ein Farbterminal und führen Sie hm lenna erneut aus.",
            Spanish: "Lenna necesita un terminal con colores ANSI activados. Abre un terminal a color y ejecuta hm lenna de nuevo.",
            Vietnamese: "Lenna cần cửa sổ dòng lệnh đã bật màu ANSI. Hãy mở cửa sổ hỗ trợ màu và chạy lại hm lenna."));
        entries.Add("Lenna.TerminalTooSmall", new(
            English: "The terminal is too small to show Lenna clearly. Enlarge the window and run hm lenna again.",
            Italian: "Il terminale è troppo piccolo per mostrare Lenna chiaramente. Allarga la finestra ed esegui di nuovo hm lenna.",
            French: "Le terminal est trop petit pour afficher Lenna clairement. Agrandissez la fenêtre et relancez hm lenna.",
            German: "Das Terminal ist zu klein, um Lenna deutlich anzuzeigen. Vergrößern Sie das Fenster und führen Sie hm lenna erneut aus.",
            Spanish: "El terminal es demasiado pequeño para mostrar Lenna con claridad. Amplía la ventana y ejecuta hm lenna de nuevo.",
            Vietnamese: "Cửa sổ dòng lệnh quá nhỏ để hiển thị rõ ảnh Lenna. Hãy mở rộng cửa sổ và chạy lại hm lenna."));
    }
}
