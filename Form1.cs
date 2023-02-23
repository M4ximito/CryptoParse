using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace test_project
{
    public partial class Form1 : Form
    {
        private TextBox mail;
        private async void button1_Click(object sender, EventArgs e)
        {
            BitcoinPriceService service = new BitcoinPriceService();
            decimal price = await service.GetBitcoinPrice("USD");
            MessageBox.Show($"1 BTC to USD is {price.ToString("C")}");
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            BitcoinPriceService service = new BitcoinPriceService();
            decimal price = await service.GetBitcoinPrice("EUR");
            MessageBox.Show($"1 BTC to EUR is {price.ToString("C")}");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            BitcoinPriceService service = new BitcoinPriceService();
            decimal price = await service.GetBitcoinPrice("PLN");
            MessageBox.Show($"1 BTC to PLN is {price.ToString("C")}");
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://blockchain.info/ticker?base=BTC";
                string response = await client.GetStringAsync(url);
                JObject data = JObject.Parse(response);

                var prices = data.Properties()
                    .Select(p => new {
                        currency = p.Name,
                        cost = (decimal)p.Value["last"]
                    })
                    .ToList();

                string json = JArray.FromObject(prices).ToString();

                // сохраняем данные в файл JSON
                string path = @".\files\bitcoin_prices.json";
                File.WriteAllText(path, json);
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
                Process.Start("explorer.exe", folderPath);


                MessageBox.Show("API successfully exported to " + path);
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://blockchain.info/ticker?base=BTC";
                string response = await client.GetStringAsync(url);
                JObject data = JObject.Parse(response);

                var prices = data.Properties()
                    .Select(p => new XElement("unit",
                        new XElement("currency", p.Name),
                        new XElement("cost", (decimal)p.Value["last"])
                    ))
                    .ToList();

                XDocument xml = new XDocument(new XElement("prices", prices));
                string path = @".\files\bitcoin_prices.xml";
                xml.Save(path);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
            openFileDialog1.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Загрузка XML файла
                    XDocument doc = XDocument.Load(openFileDialog1.FileName);

                    // Извлечение информации о валютах и ценах
                    var prices = doc.Root.Elements("unit")
                        .Select(u => new
                        {
                            currency = u.Element("currency").Value,
                            cost = decimal.Parse(u.Element("cost").Value, CultureInfo.InvariantCulture)
                        })
                        .ToList();

                    // Запись в JSON файл
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
                    saveFileDialog1.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    saveFileDialog1.RestoreDirectory = true;
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        string json = JsonConvert.SerializeObject(prices, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(saveFileDialog1.FileName, json);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
            openFileDialog1.Filter = "JSON files (*.json)|*.json";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openFileDialog1.FileName);
                JArray array = JArray.Parse(json);

                XmlDocument doc = new XmlDocument();
                XmlNode rootNode = doc.CreateElement("prices");
                doc.AppendChild(rootNode);

                foreach (JObject obj in array)
                {
                    XmlNode unitNode = doc.CreateElement("unit");
                    rootNode.AppendChild(unitNode);

                    foreach (KeyValuePair<string, JToken> property in obj)
                    {
                        XmlNode propertyNode = doc.CreateElement(property.Key);
                        propertyNode.InnerText = property.Value.ToString();
                        unitNode.AppendChild(propertyNode);
                    }
                }

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "XML files (*.xml)|*.xml";
                saveFileDialog1.Title = "Save XML file";
                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + ".xml";

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string xmlFilePath = saveFileDialog1.FileName;
                    using (XmlWriter writer = XmlWriter.Create(xmlFilePath, settings))
                    {
                        doc.Save(writer);
                    }
                }
            }
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            string filePath = @".\files\bitcoin_prices.xml";

            // Очистка таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            // Создание столбцов
            dataGridView1.Columns.Add("Currency", "Currency");
            dataGridView1.Columns.Add("Cost", "Cost");

            // Загрузка данных из XML файла
            var xml = XElement.Load(filePath);
            var units = xml.Elements("unit");
            foreach (var unit in units)
            {
                var currency = unit.Element("currency").Value;
                var cost = unit.Element("cost").Value;
                dataGridView1.Rows.Add(currency, cost);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string filePath = @".\files\bitcoin_prices.json";

            // Очистка таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            // Создание столбцов
            dataGridView1.Columns.Add("Currency", "Currency");
            dataGridView1.Columns.Add("Cost", "Cost");

            // Загрузка данных из JSON файла
            string json = File.ReadAllText(filePath);
            JArray data = JArray.Parse(json);
            foreach (JObject item in data)
            {
                string currency = (string)item["currency"];
                string cost = ((double)item["cost"]).ToString();
                dataGridView1.Rows.Add(currency, cost);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // Очистка таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string filePath = @".\files\bitcoin_prices_table.json";

            JArray data = new JArray();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string currency = row.Cells[0].Value?.ToString();
                string cost = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(currency) && !string.IsNullOrWhiteSpace(cost))
                {
                    JObject item = new JObject();
                    item.Add("currency", currency);
                    item.Add("cost", cost);
                    data.Add(item);
                }
            }

            File.WriteAllText(filePath, data.ToString());
            MessageBox.Show("Данные успешно экспортированы в JSON файл.");
        }


        private void button11_Click(object sender, EventArgs e)
        {
            string filePath = @".\files\bitcoin_prices_table.xml";

            XElement xml = new XElement("data");
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string currency = row.Cells[0].Value?.ToString();
                string cost = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(currency) && !string.IsNullOrWhiteSpace(cost))
                {
                    XElement unit = new XElement("unit");
                    unit.Add(new XElement("currency", currency));
                    unit.Add(new XElement("cost", cost));
                    xml.Add(unit);
                }
            }
            xml.Save(filePath);
        }

        public void LoadData()
        {
            // Создать подключение к MongoDB
            var connectionString = "mongodb+srv://makssanchuk377:RqTCe5Y5BhanjCCr@cluster0.ujghdfn.mongodb.net/test";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("DataCrypto"); // название базы данных

            // Получить коллекцию документов из MongoDB
            var collection = database.GetCollection<BsonDocument>("DataCrypto"); // название коллекции

            // Очистить содержимое таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            var filter = Builders<BsonDocument>.Filter.Empty;
            var documents = collection.Find(filter).ToList(); // объявление и инициализация переменной documents
            // Добавить столбцы в таблицу
            foreach (var element in documents.First().Elements)
            {
                if (element.Name != "_id")
                {
                    dataGridView1.Columns.Add(element.Name, element.Name);
                }
            }

            // Добавить строки в таблицу
            foreach (var document in documents)
            {
                var row = new DataGridViewRow();

                foreach (var element in document.Elements)
                {
                    if (element.Name != "_id")
                    {
                        var cell = new DataGridViewTextBoxCell();
                        cell.Value = element.Value;
                        row.Cells.Add(cell);
                    }
                }

                dataGridView1.Rows.Add(row);
            }
        }
        private async void button13_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            // Создать подключение к MongoDB
            var connectionString = "mongodb+srv://makssanchuk377:RqTCe5Y5BhanjCCr@cluster0.ujghdfn.mongodb.net/test";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("DataCrypto"); // название базы данных

            // Получить коллекцию документов из MongoDB
            var collection = database.GetCollection<BsonDocument>("DataCrypto"); // название коллекции

            // Очистить коллекцию документов в MongoDB
            collection.DeleteMany(Builders<BsonDocument>.Filter.Empty);

            // Добавить документы в коллекцию MongoDB из таблицы
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Пропустить последние две строки
                if (row.Index == dataGridView1.Rows.Count - 1 /*|| row.Index == dataGridView1.Rows.Count - 2*/)
                {
                    continue;
                }

                var document = new BsonDocument();

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (dataGridView1.Columns[cell.ColumnIndex].HeaderText != "_id")
                    {
                        if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                        {
                            document.Add(dataGridView1.Columns[cell.ColumnIndex].HeaderText, cell.Value.ToString());
                        }
                        else
                        {
                            document.Add(dataGridView1.Columns[cell.ColumnIndex].HeaderText, BsonNull.Value);
                        }
                    }
                }

                collection.InsertOne(document);
            }
        }



        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
