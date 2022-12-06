using System.Text;
using Newtonsoft.Json;
using static ATM.Data;

namespace ATM
{
    public class Program
    {
        static void Main(string[] args)
        {
            var data = new Data();
            data.PrintState();
            int gSum;
            for (; ; )
            {
                Console.WriteLine("Введите сумму для получения: ");
                while (!Int32.TryParse(Console.ReadLine(), out gSum) || gSum <= 0)
                {
                    Console.WriteLine("Сумма должна быть положительным числом попробуйте еще раз:");
                }
                if (data.GetMoney(gSum, out string msg))
                    Console.WriteLine(msg + "\r\nСпасибо, приходите еще!");
                else
                    Console.WriteLine($"Ошибка: {msg}");
            }
        }
    }
    /// <summary>
    /// Основной класс
    /// </summary>
    public class Data
    {
        public readonly string jsonData = Path.Combine(Path.Combine(Environment.CurrentDirectory), "data.json");
        /// <summary>
        /// Типы номиналов
        /// </summary>
        public enum EDenomination
        {
            b10=10,
            b50=50,
            b100=100,
            b200=200,
            b500=500,
            b1000=1000,
            b2000=2000,
            b5000=5000,
        }
        /// <summary>
        /// Конструктор
        /// </summary>
        public Data()
        {
            if (File.Exists(jsonData))
            {
                using (StreamReader sr = new StreamReader(this.jsonData))
                    Banknotes = JsonConvert.DeserializeObject<List<Banknote>>(sr.ReadToEnd());
            }
            if (Banknotes == null)
            {
                // Заполняем банк номиналов случайным колличеством банкнот
                Banknotes = new List<Banknote>();
                var r = new Random();
                foreach (EDenomination d in Enum.GetValues(typeof(EDenomination)))
                    Banknotes.Add(new Banknote() { Count = r.Next(1, 50), Denomination = d });
                this.Save();
            }
        }
        /// <summary>
        /// Список банкнот находящихся в банкомате
        /// </summary>
        public List<Banknote>? Banknotes { get; private set; }
        /// <summary>
        /// Метод сохранения данных в JSON
        /// </summary>
        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(jsonData, false, Encoding.UTF8))
                sw.WriteLine(JsonConvert.SerializeObject(Banknotes));
        }
        /// <summary>
        /// Вывести инфо о состоянии банкомата (кол-во номиналов и сумма)
        /// </summary>
        public void PrintState()
        {
            Console.WriteLine($"Всего в банкомате {Banknotes?.Sum(x => x.Count) ?? 0} бакноты на сумму {Banknotes?.Sum(x => x.Sum) ?? 0} у.е.");
        }
        /// <summary>
        /// Поулчить указанную сумма
        /// </summary>
        /// <param name="sum">Желаемая сумма</param>
        /// <param name="message">Сообщение с информацией о результате</param>
        /// <returns><see langword="True"/> - сумма выдана, <see langword="False"/> - Ошибка (см. <paramref name="message"/>) </returns>
        public bool GetMoney(int sum, out string message)
        {
            if (Banknotes == null || Banknotes.Sum(x=> x.Count) == 0 || Banknotes.Sum(x => x.Sum) < sum) // Проверка наличия денег в банкомате
            {
                message = "В банкомате нет достаточного колличества денег";
                return false;
            }
            var extradition = new List<Banknote>();                                                                             // Список номиналов к выдаче
            //
            foreach (var b in Banknotes.Where(x => x.Count > 0).OrderByDescending(x => x.Denomination))                         // Цикл по всем номиналам банкнот
            {
                if ((int)b.Denomination <= sum)
                {
                    int need = (int)Math.Floor((decimal)sum / (int)b.Denomination);                                             // Необходимое колличество банкнот данного номинала
                    int toIssue = need >= b.Count ? b.Count : need;                                                             // Колличнство банкнот которое будет выдано
                    sum -= (toIssue * (int)b.Denomination);                                                                     // Уменьшаем сумму к выдаче
                    extradition.Add(new Banknote() { Count = toIssue, Denomination = b.Denomination});                          // Добавляем в список выдаваемых
                    if (sum == 0)
                        break;                                                                                                  
                }
            }
            // Сумма успешно набрана, остаток 0
            if (sum == 0) 
            {
                foreach (var ddd in extradition)
                    Banknotes.First(x => x.Denomination == ddd.Denomination).Count -= ddd.Count;                                // Уменьшаем "базу" банкомата
                Save();                                                                                                         // Сохраняем в JSON
                //
                message = string.Join("\r\n", extradition.Select(x => $"{x.Count}x{(int)x.Denomination} \t= {x.Sum} у.е."));    // Формируем сообщение
                return true;
            }
            message = "Не удалось набрать подходящую сумму!";
            return false;
        }
    }
    /// <summary>
    /// Класс банкнот
    /// </summary>
    public class Banknote
    {
        /// <summary>
        /// Номинал
        /// </summary>
        public EDenomination Denomination { get; set; }
        /// <summary>
        /// Колличество
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Сумма банкнот
        /// </summary>
        public int Sum => (int)Denomination * Count;
    }
}