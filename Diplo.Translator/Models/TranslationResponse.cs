

namespace Diplo.Translator.Models
{
    public class TranslationResponse
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public string Text { get; set; }

        public string From { get; set; }

        public string To { get; set; }
    }
}
