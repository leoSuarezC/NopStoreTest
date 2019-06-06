using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Extensions;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Core.Domain.Customers;
using Nop.Custom.Domain;
using System.Collections.Specialized;
using Omikron.FactFinder.Util;

namespace Nop.Custom.Services
{
    public class CustomStoreService : ICustomStoreService
    {
        #region Constants
        private const string STORES_BY_ID_KEY = "Nop.store.id-{0}";
        private const string PRODUCTSTORES_ALLBYSTOREID_KEY = "Nop.productstore.allbystoreid-{0}-{1}-{2}-{3}-{4}";
        private const string PRODUCTSTORES_ALLBYPRODUCTID_KEY = "Nop.productstore.allbyproductid-{0}-{1}-{2}";
        private const string PRODUCTSTORES_BY_ID_KEY = "Nop.productstore.id-{0}";
        private const string STORES_PATTERN_KEY = "Nop.store.";
        private const string PRODUCTSTORES_PATTERN_KEY = "Nop.productstore.";
        private const string CUSTOMERSTORES_PATTERN_KEY = "Nop.customerstore.";
        private const string CUSTOMERSTORES_ALLBYSTOREID_KEY = "Nop.customerstore.allbystoreid-{0}-{1}-{2}-{3}";
        private const string CUSTOMERSTORES_ALLBYCUSTOMERID_KEY = "Nop.productstore.allbyproductid-{0}-{1}";
        private const string CUSTOMERSTORES_BY_ID_KEY = "Nop.customerstore.id-{0}";
        private const string CATEGORY_SEO_MAP = "Nop.category.seo.map";
        #endregion

        #region Fields

        private readonly IRepository<Store> _storeRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<AclRecord> _aclRepository;
        private readonly IWorkContext _workContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<Customer> _customerRepository;
        //private readonly IRepository<Topic> _topicRepository;
        private readonly IRepository<MPay24PaymentOptions> _mPay24PaymentOptionRepository;
        private readonly IDbContext _dbContext;
        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="storeRepository">Category repository</param>
        /// <param name="productStoreRepository">ProductCategory repository</param>
        /// <param name="productRepository">Product repository</param>
        /// <param name="eventPublisher">Event published</param>
        public CustomStoreService(ICacheManager cacheManager,
            IRepository<Store> storeRepository,
            IRepository<Product> productRepository,
            IRepository<AclRecord> aclRepository,
            IWorkContext workContext,
            IDbContext dbContext,
            IEventPublisher eventPublisher,
            IRepository<Customer> customerRepository
            //IRepository<Topic> topicRepository,
            //IRepository<MPay24PaymentOptions> mPay24PaymentOptionRepository            
            )
        {
            this._cacheManager = cacheManager;
            this._storeRepository = storeRepository;
            this._productRepository = productRepository;
            this._aclRepository = aclRepository;
            this._workContext = workContext;
            this._eventPublisher = eventPublisher;
            this._customerRepository = customerRepository;
            //this._topicRepository = topicRepository;
            //this._mPay24PaymentOptionRepository = mPay24PaymentOptionRepository;
            this._dbContext = dbContext;
        }
        #endregion
        #region Methods

        public virtual IList<MPay24PaymentOptions> GetAllMPay24PaymentOptions()
        {
            var query = _dbContext.SqlQuery<MPay24PaymentOptions>(@"select * from MPay24PaymentOptions");

            var paymentOptions = query.ToList();

            return paymentOptions;
        }
        public virtual IList<MPay24PaymentOptions> GetSelectedMPay24PaymentOptions()
        {
            var query = _dbContext.SqlQuery<MPay24PaymentOptions>(@"select o.* from MPay24PaymentOptions o 
inner join Setting s on s.Name = 'mpay24paymentsettings.paymentoptions' and s.Value like '%' + o.DisplayName + '%'
and isnull(o.DisplayName,'') <> ''
");

            var paymentOptions = query.ToList();

            return paymentOptions;
        }
        public virtual IList<SeoMap> GetCategorySeoMapping()
        {
            var listItems = _cacheManager.Get(CATEGORY_SEO_MAP, () =>
            {
                var query = _dbContext.SqlQuery<SeoMap>(@"select text_orig, text_seo from seo_map where category = 'category' order by text_orig");
                return query.ToList();
            });
            return listItems;
        }

        public virtual IList<SeoMap> GetManufacturerSeoMapping()
        {
            var listItems = _cacheManager.Get(CATEGORY_SEO_MAP, () =>
            {
                var query = _dbContext.SqlQuery<SeoMap>(@"select text_orig, text_seo from seo_map where category = 'brand' order by text_orig");
                return query.ToList();
            });
            return listItems;
        }

        public virtual SeoInfoModel GetSeoInfo(NameValueCollection sp, IList<SeoMap> categorySeoMap)
        {
            SeoInfoModel seoInfo = new SeoInfoModel() { Index = true };
            //FactFinderNavigationParams fp = GlobalSettings.FactFinderNavigationParameter;
            string[] indexThoseQueryPages = new string[] { "tieferlegung", "aufkleber" };

            if (GetParamsCount(sp) == 0)
            {
                seoInfo.MetaTitle = "Motorradbekleidung und Zubehör Online kaufen";
                seoInfo.MetaDescription = "Auner.at, der größte Onlineshop Österreichs für Motocross- und Motorard-Bekleidung sowie Zubehör. Besuchen Sie uns gerne auch in einer unseren Filialen in Wiener Neudorf oder Graz oder auf unserem beliebten auner-Flohmarkt in Hirtenberg.";
                seoInfo.H1 = "Auner Online Shop für Motorradbekleidung und Zubehör";

                // Sonderfall Paging
                if (sp["page"] != null)
                {
                    seoInfo.Index = false;
                    seoInfo.CanonicalTag = "https://www.auner.at/Onlineshop";
                }
            }
            if (sp["filterKategorie1"] != null && GetParamsCount(sp) == 1)
            { // Kategorie-Landingpages
                if (sp["filterKategorie1"] != null && sp["filterKategorie2"] == null)
                { // Kat-Ebene 1 (+ Paging)

                    if (sp["filterKategorie1"].ToLower() == "bekleidung")
                    {
                        seoInfo.MetaTitle = "Motorradbekleidung Online kaufen";
                        seoInfo.MetaDescription = "Motorradbekleidung von Auner.at, dem größten Händler Österreichs für Motorradbekleidung, Motorradhelme, Motocross-Ausstattung und mehr. Seit 40 Jahren verlässlicher Partner für Motorradfahrer.";
                        //seoInfo.MetaDescription = "Artikel der Kategorie Bekleidung im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                        seoInfo.H1 = "Motorradbekleidung";
                        seoInfo.SeoText1 = "<h2>Motorradbekleidung bei Auner</h2>Im Motorradsport ist die richtige Motorradbekleidung ein entscheidender Faktor für ein gutes Fahrgefühl mit größtmöglicher Sicherheit. Damit dieses schöne Hobby auch richtig Spaß macht, brauchst du die eine perfekte Motorradbekleidung für dich.Zusätzlich zu Lederbekleidung und Textilbekleidung findest du im Auner Online Shop für Motorradbekleidung in Österreich eine große Auswahl an Regenbekleidung und Funktionsbekleidung. Neben dem richtigen Helm, gehören auch schützende Protektoren und passende Stiefel zu jeder Motorradbekleidung dazu – wähle dazu aus einer großen Markenauswahl dein für dich maßgeschneidertes Set aus. Der passende Motorradhelm macht deine Schutzausrüstung perfekt.";
                        seoInfo.SeoText2 = "<h2>Optimale Motorradbekleidung für jede Jahreszeit</h2>Immer moderner wird der Einsatz des Zweirades auch für die kalte Jahreszeit. Bei Auners Motorradbekleidung findest du rasch die passende Funktionswäsche.Die Grundanforderung an jede Unterwäsche ist, dass sie warmhält aber auch den Schweiß absorbiert.Spezielle Fleeceeinsätze an der Motorradbekleidung sorgen für eine verstärkte Wärme an rasch auskühlenden Stellen. Dazu gehören noch passende Handschuhe: Diese können aus Gore-Tex Material sein, die für kürzere Strecken wasserdicht sind und durch die Atmung des Textils auch nach längeren Fahrten keinen Schweißgeruch haben. Lederhandschuhe dichten perfekt ab; vielfach wird Känguruh - Leder empfohlen.";
                    }
                    else
                    {
                        seoInfo.MetaTitle = "Zubehör & Technik Online kaufen";
                        seoInfo.MetaDescription = "Motorradzubehör und Technik von Auner.at, dem größten Händler Österreichs für Motorradzubehör, Technik, Motorradbekleidung und mehr. Seit 40 Jahren verlässlicher Partner für Motorradfahrer.";
                        //seoInfo.MetaDescription = "Artikel der Kategorie Zubehör & Technik im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                        seoInfo.H1 = "Zubehör & Technik";
                        seoInfo.SeoText2 = "<h3>Motorrad Zubehör & Technik</h3>Motorrad-Zubehör bietet deutlich mehr, als Funktionalität und Vernunft, denn in vielen Fällen steht die Leidenschaft dahinter, aus seinem Bike ein ganz spezielles Modell machen zu können, das den individuellen Bedürfnissen und dem persönlichen Stil gerecht wird. Mit dem Motorrad Zubehör aus dem Auner Online Shop können Biker selbst Hand anlegen und dadurch nicht nur immense Kosten für die Werkstatt sparen, sondern die Verbindung zum Bike intensivieren. Wer schon mal geschraubt hat, kennt das Gefühl und möchte es nicht mehr missen. Aber auch praktische Details aus der Auner Motorrad-Zubehör Rubrik erleichtern das Leben auf einem Zweirad, wie zum Beispiel Trinkflaschen oder Touren-Rucksäcke<h3>Motorrad Zubehör und Technik von A bis Z</h3>Der Auner Online Shop bietet eine breite Produktpalette, die von A wie Abdeckhauben bis Z wie Zündkerzen reicht. Ein umfangreiches Ersatzteil-Angebot verschiedenster Marken steht ebenso zu Verfügung, wie Tuning-Zubehör zur ganz individuellen Bike-Gestaltung. Während der eine vielleicht einen defekten Reifen oder einen Bremssatz ersetzen müssen, zielt das Interesse anderer Biker auf stilechte Details in Chrom oder besondere Kompaktlösungen. Der Auner Online Shop erlaubt die zielgerechte Suche ebenso, wie er Inspirationen durch die Zubehör-Vielfalt bietet.<h3>Motorrad Zubehör vom Profi</h3>Langjährige Erfahrungen im Motor- und vor allem im Cross- und Enduro-Bereich, haben den Auner Online Shop zu einem Spezialisten in puncto Motorrad Zubehör und Technik werden lassen. Hier weiß man, was enthusiastische Biker möchten und brauchen, um ihre Leidenschaft ausleben zu können. Damit keine Wünsche offen bleiben und Produkte durch Qualität, Vielfalt, Design und natürlich preislich begeistern, setzt der Online Shop ausschließlich auf Marken-Artikel, die für jedes Budget das Passende bieten.";
                    }

                    // Sonderfall Paging
                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;
                        seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterKategorie1=" + sp["filterKategorie1"]).GetFactFinderUrl(categorySeoMap);
                    }
                }
                else if (sp["filterKategorie2"] != null && sp["filterKategorie3"] == null)
                { // Kat-Ebene 2
                    seoInfo.MetaTitle = sp["filterKategorie2"] + "";
                    seoInfo.MetaDescription = "Artikel der Kategorie " + sp["filterKategorie2"] + " im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                    seoInfo.H1 = sp["filterKategorie2"];

                    if (sp["filterKategorie1"].ToLower() == "bekleidung")
                    {
                        if (!new string[] { "freizeit-bekleidung", "motorradstiefel-und-schuhe", "motorrad-handschuhe" }.Contains(sp["filterKategorie2"].ToLower())) // korrekt?
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie2"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie2"];
                        }

                        if (new string[] { "trial", "snowmobile" }.Contains(sp["filterKategorie2"].ToLower()))
                        {
                            seoInfo.MetaTitle = sp["filterKategorie2"] + " Bekleidung";
                            seoInfo.H1 = sp["filterKategorie2"] + " Bekleidung";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mx-bekleidung")
                        {
                            seoInfo.MetaTitle = "Motocross Bekleidung";
                            seoInfo.H1 = "Motocross-Bekleidung";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mtb")
                        {
                            seoInfo.MetaTitle = "Mountainbike Bekleidung für Downhill, Trail und All Mountain";
                            seoInfo.H1 = "MTB-Bekleidung für Downhill, Trail und All Mountain ";
                            seoInfo.SeoText2 = "<h2>MTB Bekleidung</h2>Wer mit dem Bike durch das Gelände driftet, benötigt eine entsprechende MTB Bekleidung, die ausreichend Bewegungsfreiheit bietet, den Strapazen langfristig standhält, schützt und im Idealfall auf die unterschiedlichen klimatischen Verhältnisse eingeht. Genau diese MTB Bekleidung ist im Online Shop Auner auch für kleine Budgets zu finden.<h3>MTB Bekleidung namhafter Hersteller</h3>Im Online Shop wird ein breit gefächertes Sortiment an MTB Bekleidung angeboten, das aus den Entwicklungen von renommierten Herstellern wie beispielsweise FOX, Alpinestar oder Five Ten stammt.<br />Spezialisten haben die MTB Bekleidung entworfen und füllen das Sortiment im Online Shop regelmäßig mit ihren neusten Kollektionen. Im Vordergrund steht die Funktionalität der MTB-Bekleidung, die einem MTB-Fahrer den Komfort bietet, den er für bewegungs- und belastungsintensive Touren vor allem auf unebenen Flächen benötigt. Die Funktionsbekleidung soll wie eine zweite Haut anliegen und ist dennoch so robust, dass sie über eine lang anhaltende Lebensdauer verfügt. Die MTB Bekleidung aus dem Online Shop basiert auf modernster Technologien und umfangreichen Materialforschungen, um dem stetig steigenden Anspruch von Offroad-Fahrern gerecht werden zu können.<h3>MTB Ausrüstung im Auner Online Shop</h3>Die Produktpalette im Auner Online Shop erfüllt alle Kriterien, die ein anspruchsvoller Biker an sein Outfit stellt. Gleich, ob für das BMX oder MTB, hier ist ausschließlich eine Auswahl der ersten Wahl erhältlich, die durch Qualität und vielen Funktionseigenschaften überzeugt. Von der perfekten Passform und hohem Tragekomfort, über Formstabilität und Elastizität, bis hin zu trendigen Styles und moderne Schnittformen, stehen im Auner Online Shop Unterwäsche, Socken, Hosen, Shirts, Trikots, Schuhe, Protektoren sowie Handschuhe und Helme für die ganze Familie zu Verfügung.";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "smb")
                        {
                            seoInfo.MetaTitle = "Snowmobile Bekleidung";
                            seoInfo.H1 = "Snowmobile Bekleidung";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "kinder bekleidung")
                        {
                            seoInfo.MetaTitle = "Kinder-Motorradbekleidung";
                            seoInfo.H1 = "Motorradbekleidung für Kinder und Jugendliche";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "textil-bekleidung")
                        {
                            seoInfo.SeoText1 = "<h2>Motorrad Textilbekleidung</h2>Die Textilbekleidung wurde in den letzten Jahren in ihrer Abriebfestigkeit enorm weiterentwickelt. Sie steht einer Bekleidung aus Leder kaum nach. Die Textilbekleidung sorgt für eine angenehme Durchlüftung und lässt Platz, um darunter die textile Kleidung in Schichten zu tragen.Modernste Materialien, wie die wasserdichte, winddichte und atmungsaktive GORE - TEX® Membrane sorgen dafür, dass du bei jedem Wetter und jeder Temperatur trocken bleibst, nicht auskühlst und schon gar nicht überhitzt. Ein hoher Schutz beim Motorradfahren setzt eine optimale Passform der Textilbekleidung voraus. Nicht nur die Protektoren müssen perfekt sitzen, sondern auch die Textilkombi. Wähle aus vielen Markenprodukten wie Alpinestars, Scott uvm.";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorrad-handschuhe")
                        {
                            seoInfo.SeoText2 = "<h2>Motorradhandschuhe</h2>Beim Motorradfahren zählen neben dem Kopf, die Hände zu den gefährdetsten Körperteilen.Ein Umkippen aus dem Stand reicht bereits aus, um schmerzhafte Verletzungen an den Händen hervorzurufen. Aus diesem Grund sollten Motorradhandschuhe alle Biker sowohl an kalten Tagen, wie auch während des Sommers als Schutzbekleidung ausnahmslos tragen.<br/><h3>Motorradhandschuhe - Der Basisanspruch</h3>Viele Biker verbinden mit Motorradhandschuhen den Schutz gegen frierende Hände.Doch in erster Linie sollte ein Motorradhandschuh sicherheitsrelevante Kriterien erfüllen, wie zum Beispiel Materialien aufweisen, die eine gute Griffigkeit gewährleisten, über einen ausreichend langen Armschaft verfügen, der mit der Motorradjacke problemlos überlappt, um bei einem Sturz gut zu schützen, sowie Polsterungen und gegebenenfalls Protektoren oder Verstärkungen besitzen, welche die sensibelsten Handbereiche wie Mittelfingerknochen und Ballen bei einem Unfall schützen. Diese Modelle sind im Auner Online Shop zu finden.Das Sortiment umfasst eine Produktauswahl, die sich für Hobbyfahrer ebenso eignet, wie für das Training oder den Wettkampf und entspricht den höchsten Ansprüchen.<h3>Motorradhandschuhe für den Sommer</h3>Motorradhandschuhe sind ein unbedingtes Muss auch an heißen Sommertagen. Damit aber die Hände bei hohen Temperaturen nicht schwitzen und dadurch die feste Griffigkeit im Handschuh nicht mehr gegeben sein könnte, ist es wichtig, dass Motorradhandschuhe für den Sommer über eine optimale Luftigkeit und Atmungsaktivität verfügen. Zudem kommt es auf die Materialauswahl an, die Feuchtigkeit resorbiert und dafür sorgt, dass die Hände trocken bleiben. Im Auner Online Shop sind ideale Motorradhandschuhe für Sportzwecke, als Touren-Handschuhe und speziell für Frauen zu finden, die genau diese Bedingungen an Sommer - Motorradhandschuhe erfüllen.<br/><h3>Motorradhandschuhe für den Winter</h3>Oftmals kommt es zu einem verschlechterten Griffgefühl, wenn winterliche Motorradhandschuhe zu dick gepolstert sind. Der direkte Griff - Kontakt wird dadurch minimiert und die Reaktionszeit zum Betätigen des Bremsgriffs oftmals deutlich verlängert.<br/>Warme Motorradhandschuhe müssen aber nicht dick sein, denn spezielle Thermo-Materialien können für einen teilweise noch besseren Wärmeeffekt sorgen.<br/>Der Auner Online Shop hält zahlreiche Modelle für Motorradfahrer bereit, die eine breite Auswahl an Größen, Designs, Dekoren und Farben bieten.";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "protektoren-und-nierengurte")
                        {
                            seoInfo.SeoText2 = "<h2>Protektoren für besten Körperschutz</h2>Bei Protektoren handelt es sich um Schutzausrüstung, die vor schlimmeren Verletzungen bei Motorradunfällen schützen. Diese sind in der Regel vor allem für die Körperbereiche erhältlich, die am gefährdetsten sind, wenn es zu einem Sturz kommt.<h3>Protektoren - Motorrad fahren ohne Einschränkungen</h3>Das Sortiment im Online Shop weist ausschließlich eine hochwertige Auswahl auf, die höchsten Ansprüchen gerecht werden, denn Protektoren sind nicht gleich Protektoren. Hier sind Materialien zu finden, die selbst bei extremen Belastungen Stöße auf dem Asphalt, im Gelände oder beim Zusammenstoß mit einem anderen Straßenteilnehmer entgegenwirken. Gelenke, wie beispielsweise Knie oder Schultern werden durch entsprechende Protektoren um ein Vielfaches vor Verletzungen geschützt.<h3>Protektoren mit höchsten Sicherheitsstandards</h3>Alle Modelle aus dem Online Shop entsprechen der Norm 1621-1 beziehungsweise EN 1621-2 für Rückenprotektoren. Sie schränken in der Bewegung nicht ein, sodass weder das Fahrgefühl, noch die Reaktionsfähigkeit eingeschränkt werden. Die Protektoren entstammen aus den neusten Kollektionen renommierter Hersteller und gewährleisten ein Maximum an Schutz für den Körper beim Roller-, Straßenrennen, dem Crossen oder auf der gemütlichen Sonntagstour. Abriebfeste Oberflächen, Wasserundurchlässigkeit, Wärmeisolation oder Atmungsaktivität sind Faktoren, unter denen gewählt werden kann.<h3>Protektoren Motorrad-Bedarf</h3>Qualität zu günstigen Preisen bestimmen das Sortiment des Online Shops. Ob als Einlage in die Motorradbekleidung wie Jacke und Hose, oder als separat getragenes Schutzelement, hier wird jeder fündig. Rückenprotektoren, Knie-, Ellenbogen- und Schulter-Protektoren sind ebenso im Sortiment enthalten, wie Hals- und Genickschutz-Protektoren, die vor allem im Gelände und bei Höchstgeschwindigkeiten empfehlenswert sind. Selbst Nierengurte mit Protektoren sind erhältlich, die einen ganz empfindlichen Körperbereich schützen: das Rückenmark.<br />Die Protektoren Motorrad-Bekleidung aus dem Online Shop sind ein Muss für Zweiradfahrer, um für den Fall der Fälle bestens ausgestattet zu sein. Gern können alle Artikel auch in einem der Filialen oder in den Partnergeschäften an den verschiedensten Standorten persönlich in Augenschein genommen werden.";
                        }
                    }
                    else if (sp["filterKategorie1"].ToLower() == "zubehör & technik")
                    {
                        if (sp["filterKategorie2"].ToLower() == "anbau-teile")
                        {
                            seoInfo.MetaTitle = "Motorrad Anbau-Teile";
                            seoInfo.H1 = "Anbau-Teile fürs Motorrad";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "auspuff")
                        {
                            seoInfo.MetaTitle = "Motorrad Auspuffteile";
                            seoInfo.H1 = "Motorrad Auspuffteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motor")
                        {
                            seoInfo.MetaTitle = "Motorrad-Motor und Motor-Teile";
                            seoInfo.H1 = "Motorrad-Motor und Motor-Teile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "bremse")
                        {
                            seoInfo.MetaTitle = "Motorrad-Bremse und Bremsteile";
                            seoInfo.H1 = "Motorrad-Bremse und Bremsteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "antrieb")
                        {
                            seoInfo.MetaTitle = "Motorrad-Antrieb und Antriebsteile";
                            seoInfo.H1 = "Motorrad-Antrieb und Antriebsteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "filter & dichtungen")
                        {
                            seoInfo.MetaTitle = "Motorrad-Filter und Motorrad-Dichtungen";
                            seoInfo.H1 = "Motorrad-Filter und Motorrad-Dichtungen";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "fahrwerk")
                        {
                            seoInfo.MetaTitle = "Motorrad-Fahrwerk und Fahrwerksteile";
                            seoInfo.H1 = "Motorrad-Fahrwerk und Fahrwerksteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "zubehör & gepäck")
                        {
                            seoInfo.MetaTitle = "Motorrad-Gepäck und -Zubehör";
                            seoInfo.H1 = "Motorrad-Gepäck und -Zubehör";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "werkstatt & transport")
                        {
                            seoInfo.MetaTitle = "Motorrad-Transport & Werkstatt";
                            seoInfo.H1 = "Motorrad-Transport & Werkstatt";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "rad & reifen")
                        {
                            seoInfo.MetaTitle = "Motorrad-Räder und Motorradreifen";
                            seoInfo.H1 = "Motorrad-Räder und Motorradreifen";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "elektrik")
                        {
                            seoInfo.MetaTitle = "Motorrad-Elektrik und Elektrik-Teile";
                            seoInfo.H1 = "Motorrad-Elektrik und Elektrik-Teile";
                        }
                    }

                    // Sonderfall Paging
                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;                        
                        seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterKategorie1=" + sp["filterKategorie1"] + "&filterKategorie2=" + sp["filterKategorie2"]).GetFactFinderUrl(categorySeoMap);
                    }
                }
                else if (sp["filterKategorie2"] != null && sp["filterKategorie3"] != null && sp["filterKategorie4"] == null)
                { // Kat-Ebene 3
                    seoInfo.MetaTitle = sp["filterKategorie3"] + "";
                    seoInfo.MetaDescription = "Artikel der Kategorie " + sp["filterKategorie2"] + " im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                    seoInfo.H1 = sp["filterKategorie3"];

                    if (sp["filterKategorie1"].ToLower() == "bekleidung")
                    {
                        if (sp["filterKategorie2"].ToLower() == "brillen")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-brillen")
                            {
                                seoInfo.MetaTitle = "Motocross Brillen";
                                seoInfo.H1 = "Motocross-Brillen";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "brillenzubehör")
                            {
                                seoInfo.MetaTitle = "Motorrad-" + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad-" + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "helme")
                        {
                            if (sp["filterKategorie3"].ToLower() == "moto-cross-helme")
                            {
                                seoInfo.MetaTitle = "Motocross Helme";
                                seoInfo.H1 = "Motocross Helme";
                                seoInfo.SeoText1 = "<h2>MX Helme</h2>Rein in die Kurve, ein Schlag auf die Federgabel, raus aus dem Sitz – gut abgefedert!Nun in die letzte Steilkurve im harten Gelände und ab in das Ziel – DONE!Der Motocross Helm sitzt wie angegossen für das Rennen deines Lebens im Offroad-Bereich und sieht dazu noch geil aus; der fest integrierte Kinnbereich in die Schale des MX Helms ist so optimiert, dass der Abstand zum Kinn ausreichend ist und für eine perfekte Passform sorgt. Je nach Modell und Type besitzt der Motocross Helm eine Brillenbandführung der die Motocross Brille sicher an ihrem Platz hält. Kein Beschlagen der Brille, immer frische Luft und ein bequemer Tragekomfort für den Motocrosssport.Die verschiedenen MX Helme von Auner sind optisch auf den Motorradrennsport ausgelegt und praktikabel in der Handhabung. Alternativ sind Motocross Helme auch als Vollvisierhelme erhältlich und ergänzend Protektoren, Handschuhe sowie Stiefel.Aktuelle Favoriten im Online Shop für Motocross Helme sind Airoh, Shoei, Fox und Scott – du hast die Wahl.";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorrad-handschuhe")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-handschuhe")
                            {
                                seoInfo.MetaTitle = "Motocross Handschuhe";
                                seoInfo.H1 = "Motocross-Handschuhe";
                            }
                            else
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (new string[] { "textil-bekleidung", "funktionskleidung", "regenbekleidung", "accessories" }.Contains(sp["filterKategorie2"].ToLower()))
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "leder-bekleidung")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];

                            if (sp["filterKategorie3"].ToLower() == "lederkombis")
                            {
                                seoInfo.SeoText2 = "<h2>Motorrad Lederkombi's</h2>Strapazierfähige Lederbekleidung findest du hier im Auner Online-Shop für Lederkombis in Österreich. In den Anfängen des Motorradsports war die Lederbekleidung durch eine einfache Verarbeitung sehr extremen Temperaturschwankungen ausgesetzt. Die Lederkombi im Auner Online-Shop Österreich erreicht durch innovative Technologien eine neue Ära. Materialien aus höchster Qualität, kombiniert mit einem präzisen Ventilationssystem für optimale Temperaturverhältnisse lassen das Biken zum vollen Genuss werden. Sicher beim Sturz, hält Nässe fern und schützt vor Wind. Aggressiv gestylt für die Rennstrecke oder die Straße, aus hochwertigem Vollnarbenleder in einer anatomisch geschnittenen Form liefert die Lederkombi einen einzigartigen Fahrkomfort.<br /><br /><h3>Ausstattung deiner Lederkombi</h3><br />Lederkombis gibt es aus unterschiedlichen Lederarten mit waschbarem (Mesh-) Innenfutter, herausnehmbaren CE-zertifizierten Protektoren, aerodynamisch geformten Rückenhöcker und verstärkten (Aramid) Stretcheinsätzen, die eine optimale Bewegungsfreiheit ermöglichen. Ob als Einteiler oder Lederkombi von Alpinestars und anderen namhaften Markenherstellern – hier werden Damen und Herren fündig.<br />Achte beim Kauf auf…<br />… enganliegende Produkte.<br />… exakte Passform.<br /> … entprechende Funktionen, passend zu deinen Bedürfnissen.<br /><br />Ein hoher Schutz beim Motorradfahren setzt eine optimale Passform der Lederbekleidung voraus. Bei Auner findest du qualitativ hochwertige Lederkombis, die sich auch nach vielen Ausfahrten noch in der Passform gleichen und nicht weiten. Achte darauf, dass du die Größe deiner Lederkombi eher knapp auswählst, anstatt zu groß. Nicht nur die Protektoren und Stiefel müssen perfekt sitzen, sondern auch der Abschluss des Kragenteils am Nacken soll eng anliegen, damit dir auch bei schnellen Fahrten ein Schutz vor Zugluft garantiert ist. Aufgeblasen wie ein Ballon zu fahren, weil der Halsabschluss zu weit ist, bekommt nicht gut! Bestell noch heute und probiere deine Lederkombi bequem Zuhause.";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorradstiefel-und-schuhe")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-stiefel")
                            {
                                seoInfo.MetaTitle = "Motocross Stiefel";
                                seoInfo.H1 = "Motocross-Stiefel";
                                seoInfo.SeoText2 = "<h2>Enduro- und MX-Stiefel</h2>Ein Motocross-Stiefel oder Enduro-Stiefel soll eng anliegen und fest sitzen. Trotzdem braucht der Fuß eine gewisse Bewegungsfreiheit, kombiniert mit genügend Schutz vor rauem Wetter und Stürzen. Bei einem Sturz mit deinem Bike werden laut ADAC zu 82% die unteren Gliedmaßen am meisten verletzt. Deshalb solltest du bei den MX Stiefeln auf Qualität setzen! Besonders die harten Knöchelprotektoren müssen genau am Knöchel sitzen, damit sie nicht scheuern. Im Auner Online Shop findest du nur die besten Motocross Stiefel: Ob Endurostiefel oder Trialstiefel, MX Stiefel von Alpinestars und anderen Markenherstellern – mit höchster Präzision gefertigt liefern dir die MX Stiefel hervorragenden Schutz mit verbessertem Fahrgefühl. Jeder Motocross Stiefel/Endurostiefel ist CE geprüft mit einem lässigen Design! Du findest unzählige Varianten an Motocross Stiefeln & Endurostiefeln mit eingearbeiteten Protektoren an Sohle, Ferse, Knöchel, Rist und Vorfußbereich. Spezielle MX Stiefel verfügen auch über ein Schnallenverschlusssystem, dass deine Einstellung speichert. Durch die eingebaute Schienbeinplatte am Motocross Stiefel/Endurostiefel bist du bei Stürzen optimal geschützt. Eine Verstärkung im Schalthebelbereich erleichtert dir das Schalten und verhindert das Verkrampfen des Fußes.<br /><h3>Die richtige Stiefelgröße bestimmen</h3>Stell deine Fußsohlen ohne Socken auf ein Blatt Papier. Kennzeichne die Ferse und die längste Zehe mit einem waagrechten Strich. Miss den Abstand dieser Striche. So kannst du deine Schuhgröße ermitteln! Deutsche Größenbezeichnungen sind immer in cm angegeben. Wenn deine Fußlänge 44cm beträgt, dann brauchst du Stiefelgröße 44.<br /><h3>Stiefel Größen Herren</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding=\"3px\"><tr><td><b>Deutsche Größen</b></td><td><b>Englische Größen</b></td></tr><tr><td>41</td><td>8</td></tr><tr><td>42,5</td><td>9</td></tr><tr><td>44</td><td>10</td></tr><tr><td>45</td><td>11</td></tr><tr><td>46</td><td>12</td></tr><tr><td>47</td><td>13</td></tr><tr><td>48</td><td>14</td></tr><tr><td>49</td><td>15</td></tr></table><br /><h3>Stiefel Größen Damen</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding =\"3px\"><tr><td><b>Deutsche Größen</b></td><td><b>Englische Größen</b></td></tr><tr><td>36</td><td>5</td></tr><tr><td>37,5</td><td>6</td></tr><tr><td>38,5</td><td>7</td></tr><tr><td>40</td><td>8</td></tr><tr><td>41,5</td><td>9</td></tr><tr><td>42,5</td><td>10</td></tr><tr><td>44</td><td>11</td></tr></table>";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "sport-stiefel" || sp["filterKategorie3"].ToLower() == "touren-stiefel")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                            else if (sp["filterKategorie3"].ToLower() == "halbhohe motorradstiefel")
                            {
                                seoInfo.MetaTitle = "Motorradschuhe und halbhohe Motorradstiefel";
                                seoInfo.H1 = "Motorradschuhe und halbhohe Motorradstiefel";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "protektoren & nierengurte")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstige protektoren")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mx-bekleidung")
                        {
                            seoInfo.MetaTitle = sp["filterKategorie3"].Replace("MX", "Motocross") + "";
                            seoInfo.H1 = sp["filterKategorie3"].Replace("MX", "Motocross");
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mtb")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mtb helme")
                            {
                                seoInfo.MetaTitle = "MTB Helme und Downhill Helme";
                                seoInfo.H1 = "MTB Helme und Downhill Helme";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "smb")
                        {
                            seoInfo.MetaTitle = sp["filterKategorie3"].Replace("SMB", "Snowmobile") + "";
                            seoInfo.H1 = sp["filterKategorie3"].Replace("SMB", "Snowmobile");
                        }
                        else if (sp["filterKategorie2"].ToLower() == "kinder bekleidung")
                        {
                            if (sp["filterKategorie3"].ToLower() == "kinder-protektoren")
                            {
                                seoInfo.MetaTitle = "Motorrad Kinder-Protektoren";
                                seoInfo.H1 = "Motorrad Kinder-Protektoren";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "kinder-helme")
                            {
                                seoInfo.MetaTitle = "Kinder Motorradhelme und Motocrosshelme";
                                seoInfo.H1 = "Kinder Motorrad- und Motocrosshelme";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "kinder-brillen")
                            {
                                seoInfo.MetaTitle = "Kinder Motocross-Brillen";
                                seoInfo.H1 = "Kinder Motocross-Brillen";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "bademode")
                            {
                                seoInfo.MetaTitle = "Kinder Bademode";
                                seoInfo.H1 = "Kinder Bademode";
                            }
                        }
                    }
                    else if (sp["filterKategorie1"].ToLower() == "zubehör & technik")
                    {
                        if (sp["filterKategorie2"].ToLower() == "anbau-teile")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motor")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "bremse")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstige bremsteile")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "antrieb")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "filter & dichtungen")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "fahrwerk")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "zubehör & gepäck")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "werkstatt & transport")
                        {
                            if (sp["filterKategorie3"].ToLower() != "motorradtransport")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "rad & reifen")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstiges")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                            else
                                seoInfo.Index = false;
                        }
                        else if (sp["filterKategorie2"].ToLower() == "elektrik")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                    }

                    // Sonderfall Paging
                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;
                        if (String.IsNullOrEmpty(seoInfo.CanonicalTag))                            
                            seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterKategorie1=" + sp["filterKategorie1"] + "&filterKategorie2=" + sp["filterKategorie2"] + "&filterKategorie3=" + sp["filterKategorie3"]).GetFactFinderUrl(categorySeoMap);
                    }
                }
            }
            else if (GetParamsCount(sp) == 1 && sp["filterMarke"] != null)
            { // Marken-Landingpages
                seoInfo.MetaTitle = sp["filterMarke"] + "";
                seoInfo.MetaDescription = sp["filterMarke"] + " Produkte im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                seoInfo.H1 = sp["filterMarke"];

                if (sp["filterMarke"].ToLower() == "alpinestars")
                {
                    seoInfo.SeoText1 = "<h2>Alpinestars bei Auner</h2>1963 von Santa Mazzarolo in Venezien gegründet, profiliert sich Alpinestars, übersetzt Edelweiß, seit über 50 Jahren als namhafter italienischer Produzent für Motorradbekleidung und Zubehör.Bekannt wurde Alpinestars im Motocrossrennsport als auch im Moto GP besonders durch Lederbekleidung und Protektoren.Heute wird das Unternehmen “Alpinestars Inc.“ vom Sohn des Gründers, Gabriele Mazzarolo geführt. < br />Im Auner Online-Shop findest du zusätzlich zu Bekleidung und Zubehör sowie Technik eine große Auswahl an Marken von Alpinestars. Bist du auf der Suche nach einem Geschenk? Neben dem Outfit für MX, Trial, Mountainbike, Freizeitbekleidung und Kinderbekleidung bekommst du hier auch Geschenke und Rucksäcke von Alpinestars. Ein modernes, den höchsten Qualitätsstandards entsprechendes Outfit von Alpinestars mit hoher Präzision in der Verarbeitung garantiert dir den perfekten Auftritt mit deinem Bike. Ergänzt mit coolen Stiefeln sowie griffigen Handschuhen bist du up to date. Nicht nur die Langlebigkeit von Alpinestars ist unumstritten – auch das coole Design und die hohe Performance im Rennsport sind unverkennbar.";
                }
                else if (sp["filterMarke"].ToLower() == "fox")
                {
                    seoInfo.SeoText1 = "<h2>FOX Shop für Motorrad und MTB</h2>Seit 1974 ist Fox ein beliebtes Markenprodukt im Rennsport und verbessert seine Technologie ständig. Fox liefert nicht nur höchsten Schutz und attraktive Designs für den MX Sport, sondern auch für dich, wenn du am Mountainbike downhill Vollgas gibst. Fox hält als Hersteller hochwertiger Dämpfsysteme für Mountainbikes was es verspricht und liefert Produkte für jeden Einsatz.Im Sortiment findest du Downhill - Federgabeln, Cross Country-Gabeln aber auch Freeride - und all Mountain-Gabeln.Für Fullys liefert Fox auch spezielle Luftdruckdämpfer. <h3>Produkte im Fox Online Shop</h3>Der Fox Online Shop bietet zusätzlich zu einer großen Auswahl an coolen Markenprodukten auch gezielte Informationen, damit du beim Kauf gut beraten bist.<br/>Folgende Fox Bekleidung für den Offroad Rennsport – die du im Motocross oder Mountainbike Sport nutzen kannst – findest du bei uns:<br/>Fox Helme, dazu passende Fox Brillen, Fox Handschuhe, Fox Protektoren/ Nierengurte, Fox Stiefel &Schuhe, Fox Funktionsbekleidung sowie Fox Regenbekleidung / Freizeitbekleidung / Kinderbekleidung.<br/>Mit Fox Bekleidung wählst du ein high Performance Produkt. Damit bist du garantiert sicher vor Nässe und Kälte geschützt, hast aber auch im Falle eines Sturzes den besten Schutz.Damit du aber auch schnell bist auf deinem MTB oder deiner MX, braucht deine Fox Bekleidung die perfekte Passform. Im Fox Shop findest du die beste Auswahl. Geh auf Nummer sicher und nimm jetzt, was du brauchst!";
                    seoInfo.SeoText2 = "<h2>Entstehung von FOX</h2>Im Raum Trier (Deutschland) lebte 1846 eine Familie namens Fuchs. Ihre drei Söhne übersiedelten nach Amerika – drei große Abendteurer. Einer der direkten Nachfahren war Geoff Fox, ein Physikprofessor, der mit Moto-X Fox seine eigene Modemarke entwickelte und Fox Motorradbekleidung vertrieb. 1977 gründete dieser sein eigenen Motocross-Team, das unter dem Namen Team Moto-X Fox bekannt wurde. Die Konkurrenz aus Japan wurde immer größer, aber der Hype um die handgenähten rot-orangen Tricots hielt dem Marktdruck stand. Geoff Fox musste sein Unternehmen expandieren indem er der starken Nachfrage mit Massenproduktion gerecht wurde. Seit dem sponsert Fox namhaft Motocrossfahrer wie Mark Barnett (1980), Brad Lackey (1982), Ricky Charmichael und James Stewart in den 2000er Jahren.";
                }
                else if (sp["filterMarke"].ToLower() == "acerbis")
                {
                    seoInfo.SeoText2 = "<h2>Acerbis</h2>Der Spezialist auf den Gebieten des Motocross und Enduro Bekleidung sowie Zubehör, präsentiert sich der italienische Hersteller Acerbis. Eine Top-Qualität erwartet ambitionierte Dirty-Biker, denen eine umfangreiche Auswahl an Acerbis Bekleidung sowie Zubehör zur Auswahl steht. Seit 1973 steht der Herstellername für kompromisslose Sicherheit, außergewöhnliche Designs sowie optimale Funktionalität und zeichnet sich durch Spitzen-Qualität in allen Bereichen aus, die sich über das Acerbis Zubehör und Schutzkleidung erstreckt.<h3>Acerbis Bekleidung</h3>Im Online Shop finden Offroad-Fans alles an Acerbis-Bekleidung, was sie für ihre Touren im Gelände oder auf der Straße benötigen. Protektorenjacken sowie -hosen oder mit separaten Protektoren-Taschen, Brustpanzer und Nackenschutzsysteme, Cross- und Enduro-Stiefel oder Helme sind nur eine kleine Auswahl von dem, was Acerbis-Fans im Online Shop erwartet. Die Acerbis Bekleidung besticht durch exzellente Verarbeitung, die höchsten Tragekomfort versprechen. Flexible Materialien erlauben eine uneingeschränkte Bewegungsfreiheit, die beispielsweise Kicker mit Leichtigkeit nehmen lassen.<h3>Acerbis Zubehör</h3>Das Acerbis Zubehör erstreckt sich von praktischen Details wie zum Beispiel Trink-Rucksäcke oder Nierentaschen. Weiteres Zubehör wie Benzin-(Zusatz-)Tanks und Benzinumleitungen, Gabelschutz, Motorschutzplatten, Plastik-Kids, Sitzbänke und Scheinwerfer oder Ersatzteile wie Tankdeckel und Luftfilterabdeckungen sind ebenfalls im Acerbis-Sortiment des Online Shops enthalten. Ein Blick auf das Acerbis-Zubehör lohnt sich auf jeden Fall.<h3>Acerbis jetzt im Online Shop</h3>Das Besondere an Acerbis ist der direkte Bezug zum Offroad Motorrad-Rennsport. Auf den Dakar Rallys, Enduro World Championships, USA Supercross Championships sowie USA National Championships und European Cross Country ist Acerbis stets präsent. Langjährige Erfahrungen und eine enge Zusammenarbeit mit Weltmeistern, habe aus Acerbis eine Marke gemacht, die keine Wünsche offen lässt und nun bei Auner zu Top-Preisen für Offroader ebenso, wie für Asphaltfahrer erhältlich ist.";
                }

                // Sonderfall Paging
                if (sp["page"] != null)
                {
                    seoInfo.Index = false;                    
                    seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterMarke=" + sp["filterMarke"]).GetFactFinderUrl(categorySeoMap);

                }
            }
            else if (GetParamsCount(sp) == 2 && sp["filterKategorie1"] != null && sp["filterMarke"] != null)
            { // Kategorie-Marken-Landingpages
                if (sp["filterKategorie2"] == null) // Kat1 + Marke
                {
                    seoInfo.MetaTitle = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie1"]) + "";
                    seoInfo.MetaDescription = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie1"]) + " im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                    seoInfo.H1 = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie1"]);

                    // Sonderfall Paging
                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;
                        seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterMarke=" + sp["filterMarke"] + "&filterKategorie1=" + sp["filterKategorie1"]).GetFactFinderUrl(categorySeoMap);                        
                    }
                }
                else if (sp["filterKategorie2"] != null && sp["filterKategorie3"] == null) // Kat2 + Marke
                {
                    seoInfo.MetaTitle = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie2"]) + "";
                    seoInfo.MetaDescription = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie2"]) + " im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                    seoInfo.H1 = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie2"]);

                    if (sp["filterKategorie1"].ToLower() == "bekleidung")
                    {                        
                        if (new string[] { "trial", "snowmobile" }.Contains(sp["filterKategorie2"].ToLower()))
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie2"] + " Bekleidung";
                            seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie2"] + " Bekleidung";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mx-bekleidung")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motocross Bekleidung";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motocross-Bekleidung";

                            if (sp["filterMarke"].ToLower() == "fox")
                            {
                                seoInfo.SeoText2 = "<h2>Fox MX Bekleidung</h2>Seit mehr als dreißig Jahren liefet Fox MX Bekleidung und MTB Bekleidung in hochwertiger Qualität für die heutigen Amateur-MX Fahrer als auch für die Profi-MX Piloten weltweit. Du hast die Wahl zwischen Fox MX Shirts oder Fox MX Hosen für dein Rennen bzw. deine Ausfahrt ins Gelände. Auch Damen finden hier alles über Fox MX Bekleidung für den Motocrosssport. Passgenaue Damen Fox MX Hosen und Damen Fox MX Shirts für die perfekte Offroad Fahrt - damit bei jedem Sprung alles sitzt!<br /><h3>Was bietet die Fox MX Bekleidung?</h3>Anfangs verfügte Fox über ein kleines Sortiment an handgenähter Fox MX Bekleidung/Fox Motocrossbekleidung. Mit den Jahren kam der Erfolg und die Nachfrage wurde größer. Fox spezialisierte sich auf funktionelle Fox MX Bekleidung/Fox Motocrossbekleidung. Modisch on top gilt Fox MX Bekleidung/Fox Motocrossbekleidung als legendär. Fox MX Bekleidung ist mit hochwertigen Materialien auf dem neuesten Stand der Technik. Durch die innovative Forschung ist Fox MX Bekleidung/Fox Motocrossbekleidung die beliebteste Marke im Motocrosssport. Das feuchtigkeitstransportierende Material sorgt für verstärkte Leistungsfähigkeit bei maximalem Tragekomfort. Durch atmungsaktive Seitenpanels ist eine optimale Luftzirkulation garantiert. Einsätze mit Mesh-Innenfutter ermöglichen eine verstärkte Feuchtigkeitsabsorption an den Stellen, an denen du verstärkt schwitzt. Im Auner Online Shop findest du auch Protektoren und Nierengurte, sowie Motorradbekleidung zum Trial fahren. Fox MX Bekleidung ist auch heute noch die meistverkaufte Marke für Motocrossbekleidung. Probiere deine Fox MX Bekleidung/Fox Motocrossbekleidung bequem Zuhause aus. Wähle noch heute aus unzähligen Angeboten und Aktionen deine unverkennbare Fox MX Bekleidung.";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mtb")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Mountainbike Bekleidung für Downhill, Trail und All Mountain";
                            seoInfo.H1 = sp["filterMarke"] + " " + "MTB-Bekleidung für Downhill, Trail und All Mountain ";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "smb")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Snowmobile Bekleidung";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Snowmobile Bekleidung";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "kinder bekleidung")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Kinder-Motorradbekleidung";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorradbekleidung für Kinder und Jugendliche";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "helme" && sp["filterMarke"].ToLower() == "fox")
                        {
                            seoInfo.SeoText1 = "<h2>Fox Helme</h2>Der Wind fetzt dir um den Helm; der letzte Sprung und schon hast du deine Fahrt perfekt gemeistert!Darf´s ein moderner Fox Helm sein für deine MX? Bei Fox Helmen kannst du dir sicher sein, dass sie halten, was sie versprechen und zudem auch noch in fetzigen Farben und lässigen Designs zu kaufen sind.<br/>Neben einem tollen Design soll der Fox Helm weder drücken, noch zu locker sitzen. Das alles hängt von deiner Kopfform ab.Wir haben einige Tipps für dich zusammengestellt, auf die du beim Kauf achten sollst.<br/>Die wichtigste Grundregel beim Helmkauf ist, auf die ECE - Etikette zu achten. Alle Fox Helme bei Auner sind nach der ECE 22 - 05 Norm gefertigt und entsprechen so den besten Qualitätsstandards. Beginn mit der Messung deines Kopfumfanges. Trägst du eine Brille, Ohrstöpsel oder Sturmhaube, trage diese beim Messen auch. Lege das Maßband einen Zentimeter über den Ohren zu den Augenbrauen rund um den Kopf.Liegt das Band eng an, schneidet dich aber nicht ein, dann hast du richtig gemessen.In der Größenauswahl oben findest du die Größe des Kopfumfangs und kannst einfach deine richtige Fox Helmgröße auswählen.<br/>Beim Verschluss kannst du darauf achten, dass der Kinnriemen fest sitzt.Wenn sich der Kinnriemen mit Steckschloss mit der Zeit weitet, sollte er nachjustiert werden.Der Fox Helmverschluss mit der Ratsche funktioniert anfänglich etwas umständlicher, ist aber ein sehr sicheres Produkt.Der Doppel-D Verschluss ist etwas leichter als die Ratsche und ist stufenlos einstellbar.<br/>Bestell noch heut für die nächste Ausfahrt deinen Fox Helm!";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "helme" && sp["filterMarke"].ToLower() == "airoh")
                        {
                            seoInfo.SeoText1 = "<h2>Airoh Helme</h2>Nach Firmengründung im Jahre 1986 war der heutige Airoh Helme Hersteller als Zulieferer renommierter Motorradhelm-Unternehmen tätig, bis er 1997 sein erstes eigenes Modell vorstellte.<br />Seither konnte das in Italien ansässige Familienunternehmen durch technische Kompetenz, langjährige Erfahrungen und ihrer Leidenschaft zum Motorsport, den Namen Airoh weltweit zu einer renommierten Größe machen.<h3>Freiheit und Schutz</h3>Airoh Helme bestechen durch Entwicklungen, die bis ins Detail durchdacht sind. Hohe Sicherheitsstands, Qualität, moderne Optik sowie Tragekomfort, der das Freiheitsgefühl des Motorradfahrens nicht einschränkt, stehen bei der Planung der Airoh Helme im Fokus.<br />Täglich steigt der Anspruch und damit die Ziele des Herstellers, um Motorradliebhaber sowohl aus dem Freizeit- als auch aus dem Profi-Sportbereich zufriedenstellen zu können.<h3>Airoh Helme in großer Auswahl</h3>Die Auswahl umfasst ein breites Sortiment, das Jethelme, Racing- und Integralhelme, Klapp- sowie Crosshelme für jeden Anspruch bereithält. Frische Designs und auffällige Dekore schaffen den hohen Wiedererkennungswert der Airoh Helme. Wer es unauffälliger mag, kann auf Universal-Farben zurückgreifen.<h3>Airoh Helme für sportliche Fahrer</h3>Insbesondere für sportliche Fahrer aus dem Motocross-, Enduro-, Straßenrenn- und Trailsport, eignen sich die Airoh Helme. Der Hersteller kann bereits auf 86 Weltmeistertitel zurückblicken, die mit ihren Spitzenprodukten erreicht wurden. Ob die Genfer Motocrossfahrerin Virginie Germond, Matthew Philipps oder Taddy Blazusiak, sie und viele andere Profi-Motorsportler setzen auf Airoh Helme, weil Ihnen Komfort, Bedienungsfreundlichkeit sowie Funktionalität und ein geringes Gewicht ebenso wichtig sind, wie Sicherheit.<br />Für den anspruchsvollen, leidenschaftlichen Motorradfahrer sind die italienischen Airoh Helme wie geschaffen und überzeugen zudem zusätzlich durch preisgünstige Angebote im Online Shop.";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "helme" && sp["filterMarke"].ToLower() == "hjc")
                        {
                            seoInfo.SeoText1 = "<h2>HJC Helme</h2>Der Hersteller von HJC Helmen ist seit den siebziger Jahren ein Unternehmen, das über eines der größten internationalen Vertriebsnetze verfügt. Der Name steht für Innovationen, Qualitätsprodukte, Funktionalität sowie aus der Masse herausstechende Designs.<br />Profi-Motorrad-Sportler wie der MotoGP Weltmeister Jorge Lorenzo sowie der US-amerikanische Weltklasse Motocrossfahrer Nate Adams sind nur zwei bekannte Beispiele, die auf HJC Helme setzen.<h3>HJC Helme für jeden Anspruch</h3>Ob als Hobby-Biker, Motocrosser, Chopper-Fahrer oder für den Alltag, das Sortiment der HJC Helme bietet für jeden Anspruch und Geschmack das passende Modell. Diese erstrecken sich über die verschiedensten Modellarten von dem Integralhelm auch in der Sportversion, über HJC Crosshelme und Klapphelme bis hin zu Jet- und Rollerhelmen sowie spezielle Modelle für Frauen.<h3>HJC Helme - immer einen Schritt voraus</h3>Die Entwicklung der HJC Helme basiert stets auf neusten technischen Erkenntnissen, um dem Kopf den größtmöglichen Schutz bieten zu können, und gleichzeitig einen maximalen Tragekomfort zu ermöglichen. Die HJC Helme verbinden gut durchdachte Detaillösungen mit hochwertigen Materialien, spritzigen Formen sowie modernen Dekoren. Mehrlagige Verbunde mit Verstärkungen aus Carbon oder Carbon Glass Hybrid-Fasern für mehr Schlagdämpfung, integrierte Sonnenblenden, leichtes Gewicht und innovative Polsterkonzepte für mehr Tragekomfort sind in den neusten Modellen zu finden.<h3>HJC Helme mit optimalem Preis-Leistungs-Verhältnis</h3>HJC Helme bestechen seit vielen Jahren durch faire Preis-Leistungs-Verhältnisse und sind der Beweis dafür, dass sich eine sportlich-stilvolle Optik mit höchsten Sicherheitsfaktoren perfekt kombinieren lassen. Wem seine Gesundheit mehr als nur einen klassischen Standardhelm wert ist, findet hier HJC Helme, bei denen trotz günstiger Preise keine Kompromisse in allen Bereichen einzugehen sind.";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "freizeit-bekleidung" && sp["filterMarke"].ToLower() == "fox")
                        {
                            seoInfo.SeoText2 = "<h2>Fox Freizeitbekleidung</h2>Brandaktuelle, neue Teile aus der Fox Streetwear Collection. Fox Freizeitbekleidung ist der Hype in der Modebranche. Viel Teile der Fox Freizeitbekleidung auch in Aktion. Du findest hier Fox T-Shirts, Fox Hauben & Fox Kappen, Fox Pullover und Fox Hoodies für Damen und Herren. Zusätzlich bieten wir im Auner Online Shop sportliche Fox Bikinis für Damen und coole Fox Surfshorts für Herren. Dazu gibt es passende Fox Accessoires als perfektes Detail für deinen Auftritt. Wähle deine Sparte und finde Fox Freizeitbekleidung in allen Variationen. Unter Extremsportlern ist Fox Bekleidung die beliebteste und meistverkaufte Sportmarke. Doch nicht nur hardcore Sportler lieben Fox Freizeitbekleidung. Auch für den Alltag ist die Fox Freizeitbekleidung genial. Angelehnt an die sportlichen Bedürfnisse wird die Fox Freizeitbekleidung so hergestellt, dass sie rasch trocknet. Zudem verspricht sie eine grenzenlose Bewegungsfreiheit und hat ein cooles Styling, sodass du dich beim Kauf kaum entscheiden kannst! Hochwertige Stoffe werden für die Verarbeitung der Fox Freizeitbekleidung verwendet und erlebst einen traumhaften Tragekomfort.<br />Du suchst für die Freizeit, deinen Sport oder das Baden die richtige Kleidung? Mit Fox Freizeitbekleidung liegst du voll im Trend. Bestell dir noch heute deine Fox Freizeitbekleidung und du garantiert fällst auf!";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorrad-handschuhe" && sp["filterMarke"].ToLower() == "fox")
                        {
                            seoInfo.SeoText2 = "<h2>Fox Handschuhe</h2>Ein guter Sitz mit wasserdichtem und atmungsaktivem Gewebe ist dir mit dem Fox Handschuh garantiert. Das beste Material ermöglicht dir ein feines Fahrgefühl und ein tolles Griffgefühl an deinem Lenker. Wähle zwischen Fox Lederhandschuhen, Fox Handschuhe für Schlechtwetter oder Fox Sporthandschuhen. Damit bist du garantiert geschützt bei deiner Ausfahrt! Materialien wie Neopren, Polar Fleece Innenfutter und Fox Gel Grip an der Handinnenfläche ermöglichen dir den perfekten Grip auf deinem Lenker. Deine Fox Handschuhe gibt es auch mit touchscreenfähigem Material auf den Fingerkuppen und Knöchelverstärkungen. Mit einem Fox Handschuh hast du eine hochwertige Schutzfunktion durch die innovative Fox Technologie. Verstärkungen an den Knöcheln, Protektoren an den heiklen Stellen und ein fest sitzendes Klettband sorgen für höchste Sicherheit.<br /><h3>Richtige Handschuh-Größe</h3>Miss mit einem Maßband an der breitesten Stelle deiner Handfläche. Lass dabei den Daumen frei und zieh das Maßband oberhalb des Daumens rund um deine vier Finger. So erhältst du die optimale Größe für deinen Handschuh. Wähle in der Größenauswahl deinen passenden Fox Handschuh aus.<br /><h3>FOX Handschuh-Größentabelle</h3><table style=\"min-width:320px; margin-top:4px\" cellpadding=\"3px\"><tr><td><b>Bestellgröße</b></td><td><b>US-Größe</b></td><td><b>Handinnenfläche in cm</b></td></tr><tr><td>S</td><td>8</td><td>18,2-18,8</td></tr><tr><td>M</td><td>9</td><td>18,8-19,4</td></tr><tr><td>L</td><td>10</td><td>19,4-20,0</td></tr><tr><td>XL</td><td>11</td><td>20,0-20,6</td></tr><tr><td>XXL</td><td>12</td><td>20,6-21,2</td></tr><tr><td>3XL</td><td>13</td><td>21,2-21,8</td></tr><tr><td>4XL</td><td>14</td><td>21,8-22,4</td></tr></table>";
                        }
                    }
                    else if (sp["filterKategorie1"].ToLower() == "zubehör & technik")
                    {
                        if (sp["filterKategorie2"].ToLower() == "anbau-teile")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad Anbau-Teile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Anbau-Teile fürs Motorrad";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "auspuff")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad Auspuffteile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad Auspuffteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motor")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-Motor und Motor-Teile";
                            seoInfo.H1 = sp["filterMarke"] + " " + " Motor-Teile fürs Motorrad";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "bremse")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Bremsteile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Bremsteile und Bremsen fürs Motorrad";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "antrieb")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Antriebsteile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Antriebsteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "filter & dichtungen")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-Filter und Motorrad-Dichtungen";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad-Filter und Motorrad-Dichtungen";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "fahrwerk")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Fahrwerksteile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Fahrwerksteile";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "zubehör & gepäck")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-Gepäck und -Zubehör";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad-Gepäck und -Zubehör";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "werkstatt & transport")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-Transport & Werkstatt";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad-Transport & Werkstatt";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "rad & reifen")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-Räder und Motorradreifen";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad-Räder und Motorradreifen";
                        }
                        else if (sp["filterKategorie2"].ToLower() == "elektrik")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + "Elektrik-Teile";
                            seoInfo.H1 = sp["filterMarke"] + " " + "Elektrik-Teile";
                        }
                    }

                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;
                        seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterMarke=" + sp["filterMarke"] + "&filterKategorie1=" + sp["filterKategorie1"] + "&filterKategorie2=" + sp["filterKategorie2"]).GetFactFinderUrl(categorySeoMap);                       
                    }
                }
                else if (sp["filterKategorie2"] != null && sp["filterKategorie3"] != null && sp["filterKategorie4"] == null) // Kat3 + Marke
                {
                    seoInfo.MetaTitle = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie3"]) + "";
                    seoInfo.MetaDescription = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie3"]) + " im Auner Online Shop für Motorradbekleidung, Motorrad-Zubehör und Motorrad-Teile. Hier gibt es das gesamte Sortiment der Firma Auner online zu kaufen.";
                    seoInfo.H1 = sp["filterMarke"] + " " + UeberschriftSeoAnpassung(sp["filterKategorie3"]);

                    if (sp["filterKategorie1"].ToLower() == "bekleidung")
                    {
                        if (sp["filterKategorie2"].ToLower() == "brillen")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-brillen")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motocross Brillen";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motocross-Brillen";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "brillenzubehör")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad-" + sp["filterKategorie3"] + "";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad-" + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "helme")
                        {
                            if (sp["filterKategorie3"].ToLower() == "moto-cross-helme")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motocross Helme";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motocross Helme";

                                if (sp["filterMarke"].ToLower() == "fox")
                                {
                                    seoInfo.SeoText1 = "<h2>Fox Motocross Helme</h2>Darf´s ein Fox Motocross Helm oder ein Fox Enduro Helm sein? Bei Fox Helmen kannst du dir sicher sein, dass er hält, was er verspricht.Eine perfekte Passform wird durch spezielle Wangenpolster in einer Carbonschale möglich, wenn du den besten Enduro oder MX Helm suchst.Wähle nach deinen Bedürfnissen aus!Der Fox Helm braucht eine optimale Belüftung, damit du auf deiner Motocross Gas geben kannst.Auslasskanäle, waschbares, antiallergisches Innenfutter, ein optimales Preis-Leistungsverhältnis sind beim Fox Helm kombiniert mit extrem leichtem Fibermaterial auf den neuesten Sicherheitsstandards nach der ECE 22 - 05 Zulassung.<br/>Das neueste Mips - System garantiert höchste Sicherheit bei einem möglichen Sturz.Rüste dich aus, damit du stets einen kühlen Kopf bewahren kannst. Immer wieder gibt es Fox Motocross und Enduro Helme als limitierte Sonderedition, die deinen Auftritt auf deinem Offroad Bike perfekt macht!";
                                    seoInfo.SeoText2 = "<h2>Fox Motocross Helmgrößen</h2>Bestell noch heut für die nächste Ausfahrt deinen Fox Helm!<br /><h3>FOX Helmgrößen</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding=\"3px\" ><tr><td><b>Bestellgröße</b></td><td><b>Kopfumfang in cm</b></td></tr><tr><td>XXS</td><td>51,5-52</td></tr><tr><td>XS</td><td>52,5-54</td></tr><tr><td>S</td><td>54,5-56</td></tr><tr><td>M</td><td>56,5-58</td></tr><tr><td>L</td><td>58,5-60</td></tr><tr><td>XL</td><td>60,5-62</td></tr><tr><td>XXL</td><td>62,5-64</td></tr><tr><td>3XL</td><td>64,5-66</td></tr></table>";
                                }
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorrad-handschuhe")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-handschuhe")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motocross Handschuhe";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motocross-Handschuhe";
                            }
                            else
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (new string[] { "leder-bekleidung", "textil-bekleidung", "funktionskleidung", "regenbekleidung", "accessories" }.Contains(sp["filterKategorie2"].ToLower()))
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motorradstiefel-und-schuhe")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mx-stiefel")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motocross Stiefel";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motocross-Stiefel";

                                if (sp["filterMarke"].ToLower() == "fox")
                                {
                                    seoInfo.SeoText2 = "<h2>Fox MX Stiefel</h2>Gute Fox Stiefel sind in einem coolen Design mit Protektoren und teils auswechselbaren Schleifern für das Hanging-off. Passende Fox Stiefel müssen knapp sitzen, dürfen aber nicht drücken. Sie sollen deine Füße gut belüften und dennoch bei Nässe das Wasser fernhalten. Wähle aus unterschiedlichen Materialien mit verschiedenen Funktionen deinen perfekten Fox Stiefel aus. Damit der Tragekomfort auch bei langen Ausfahrten hervorragend bleibt, findest du hier Informationen, wie du die Stiefelgröße messen kannst.<br /><h3>Wie misst du deine Fox Stiefel/Fox Schuhe?</h3>Stell dich mit nackten Beinen auf ein Blatt Papier. Markiere deine Ferse und deine längste Zehe mit je einem waagrechten Strich. Miss den Abstand beider Striche genau und vergleiche mit den Größenangaben in der Filterauswahl. Beachte, dass die Größen zwischen den Fox Motorradstiefel und den Fox Schuhen unterschiedlich sind.<br /><h3>FOX Stiefel Größen Herren</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding =\"3px\" >    <tr><td><b>Deutsche Größen</b></td><td><b>Englische Größen</b></td></tr>    <tr><td>41</td><td>8</td></tr>    <tr><td>42,5</td><td>9</td></tr>    <tr><td>44</td><td>10</td></tr>    <tr><td>45</td><td>11</td></tr>    <tr><td>46</td><td>12</td></tr>    <tr><td>47</td><td>13</td></tr>    <tr><td>48</td><td>14</td></tr>    <tr><td>49</td><td>15</td></tr></table><br /><h3>FOX Stiefel Größen Damen</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding=\"3px\" >    <tr><td><b>Deutsche Größen</b></td><td><b>Englische Größen</b></td></tr>    <tr><td>36</td><td>5</td></tr>    <tr><td>37,5</td><td>6</td></tr>    <tr><td>38,5</td><td>7</td></tr>    <tr><td>40</td><td>8</td></tr>    <tr><td>41,5</td><td>9</td></tr>    <tr><td>42,5</td><td>10</td></tr>    <tr><td>44</td><td>11</td></tr></table>";
                                }
                            }
                            else if (sp["filterKategorie3"].ToLower() == "sport-stiefel" || sp["filterKategorie3"].ToLower() == "touren-stiefel")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie3"];
                            }
                            else if (sp["filterKategorie3"].ToLower() == "halbhohe motorradstiefel")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Motorradschuhe und halbhohe Motorradstiefel";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Motorradschuhe und halbhohe Motorradstiefel";
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "protektoren & nierengurte")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstige protektoren")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mx-bekleidung")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie3"].Replace("MX", "Motocross") + "";
                            seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie3"].Replace("MX", "Motocross");
                        }
                        else if (sp["filterKategorie2"].ToLower() == "mtb")
                        {
                            if (sp["filterKategorie3"].ToLower() == "mtb helme")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "MTB Helme und Downhill Helme";
                                seoInfo.H1 = sp["filterMarke"] + " " + "MTB Helme und Downhill Helme";

                                if (sp["filterMarke"].ToLower() == "fox")
                                {
                                    seoInfo.SeoText2 = "<h2>Fox MTB Helm</h2>Du suchst einen BMX/Mountainbike Helm für deinen perfekten Ride? Hier bist du richtig. Wähle aus unterschiedlichen Varianten und Designs einen hochentwickelten Fox MTB Helm mit bestem Tragekomfort. Durch präzise Luftkanäle wird eine optimale Be- und Entlüftung ermöglicht. Du suchst nach der richtigen Helmgröße für deinen Fox MTB Helm?<br /><h3>Passform</h3>Miss mit einem Maßband oberhalb der Ohren und in etwa 1-2 cm über den Augenbrauen rund um deinen Kopf. Du findest die Größenauswahl in der Filterfunktion oben. Der Fox MTB/BMX Helm sollte mit den Augenbrauen abschließen, sofern du einen Fox BMX oder MTB Helm kaufst. Generell sollte der Fox MTB Helm nicht schief oder zu tief im Nacken sitzen. Wenn der Kinnriemen geschlossen ist, soll der Fox MTB Helm eng am Kinn anliegen. Wenn du deinen neun Fox MTB Helm geliefert bekommst, stell die Riemen so ein, dass sie eng an den Wangen liegen. Bestell dir noch heute deinen Fox MTB Helm und probiere in Zuhause bequem an!<h3>FOX Helmgrößen</h3><table style=\"min-width:320px;margin-top:4px\" cellpadding=\"3px\" ><tr><td><b>Bestellgröße</b></td><td><b>Kopfumfang in cm</b></td></tr><tr><td>XXS</td><td>51,5-52</td></tr><tr><td>XS</td><td>52,5-54</td></tr><tr><td>S</td><td>54,5-56</td></tr><tr><td>M</td><td>56,5-58</td></tr><tr><td>L</td><td>58,5-60</td></tr><tr><td>XL</td><td>60,5-62</td></tr><tr><td>XXL</td><td>62,5-64</td></tr><tr><td>3XL</td><td>64,5-66</td></tr></table>";
                                }
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "smb")
                        {
                            seoInfo.MetaTitle = sp["filterMarke"] + " " + sp["filterKategorie3"].Replace("SMB", "Snowmobile") + "";
                            seoInfo.H1 = sp["filterMarke"] + " " + sp["filterKategorie3"].Replace("SMB", "Snowmobile");
                        }
                        else if (sp["filterKategorie2"].ToLower() == "kinder bekleidung")
                        {
                            if (sp["filterKategorie3"].ToLower() == "kinder-protektoren")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Kinder-Protektoren";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Kinder-Protektoren";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "kinder-helme")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Kinder Motorradhelme und Motocrosshelme";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Kinder Motorrad- und Motocrosshelme";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "kinder-brillen")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Kinder Motocross-Brillen";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Kinder Motocross-Brillen";
                            }
                            else if (sp["filterKategorie3"].ToLower() == "bademode")
                            {
                                seoInfo.MetaTitle = sp["filterMarke"] + " " + "Kinder Bademode";
                                seoInfo.H1 = sp["filterMarke"] + " " + "Kinder Bademode";
                            }
                        }
                    }
                    else if (sp["filterKategorie1"].ToLower() == "zubehör & technik")
                    {
                        if (sp["filterKategorie2"].ToLower() == "anbau-teile")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "motor")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "bremse")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstige bremsteile")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "antrieb")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "filter & dichtungen")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "fahrwerk")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "zubehör & gepäck")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        else if (sp["filterKategorie2"].ToLower() == "werkstatt & transport")
                        {
                            if (sp["filterKategorie3"].ToLower() != "motorradtransport")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                        }
                        else if (sp["filterKategorie2"].ToLower() == "rad & reifen")
                        {
                            if (sp["filterKategorie3"].ToLower() != "sonstiges")
                            {
                                seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                                seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                            }
                            else
                                seoInfo.Index = false;
                        }
                        else if (sp["filterKategorie2"].ToLower() == "elektrik")
                        {
                            seoInfo.MetaTitle = "Motorrad " + sp["filterKategorie3"] + "";
                            seoInfo.H1 = "Motorrad " + sp["filterKategorie3"];
                        }
                        seoInfo.MetaTitle = sp["filterMarke"] + " " + seoInfo.MetaTitle;
                        seoInfo.H1 = sp["filterMarke"] + " " + seoInfo.H1;
                    }

                    // Sonderfall Paging
                    if (sp["page"] != null)
                    {
                        seoInfo.Index = false;
                        seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterMarke=" + sp["filterMarke"] + "&filterKategorie1=" + sp["filterKategorie1"] + "&filterKategorie2=" + sp["filterKategorie2"] + "&filterKategorie3=" + sp["filterKategorie3"]).GetFactFinderUrl(categorySeoMap);
                    }
                }
            }
            else if (GetParamsCount(sp) == 1 && sp["filterAngebotArt"] != null)
            { // Schnäppchen
                seoInfo.MetaTitle = "Ermäßigte Motorradbekleidung und Zubehör bei Auner";
                seoInfo.H1 = "Motorradbekleidung und Zubehör Sonderangebote";
                seoInfo.MetaDescription = "Alle unsere ermäßigten Artikel und Abverkaufsangebote an Motorradbekleidung und Motorradzubehör auf einen Blick. Besonders günstig einkaufen. Jetzt haben Sie die Chance.";

                // Sonderfall Paging
                if (sp["page"] != null)
                {
                    seoInfo.Index = false;
                    seoInfo.CanonicalTag = "https://www.auner.at" + ("/OnlineShop?filterAngebotArt=" + sp["filterAngebotArt"]).GetFactFinderUrl(categorySeoMap);                    

                }
            }            
            else if (GetParamsCount(sp) >= 2)
            {
                seoInfo.Index = false;
            }

            // Alle Spezialfälle mit Filtern, Sortierung etc. 
            if (sp["page"] != null)
                seoInfo.Index = false;
            //if (!String.IsNullOrEmpty(sp.Preis))
            //    seoInfo.Index = false;
            if (!string.IsNullOrWhiteSpace(sp["filterFilterFarbe"])) // > 1 erst wenn ich die Farb-Seiten optimiere
                seoInfo.Index = false;
            //if (sp.Groesse != null && sp.Groesse.Count > 0)
            //    seoInfo.Index = false;
            //if (!String.IsNullOrEmpty(sp.SortColumnName))
            //    seoInfo.Index = false;
            //if (!String.IsNullOrEmpty(sp.SortOrder))
            //    seoInfo.Index = false;
            if (!String.IsNullOrEmpty(sp["query"]) && sp["query"] != "*" && !indexThoseQueryPages.Contains(sp["query"].ToLower()))
                seoInfo.Index = false;
            return seoInfo;
        }
        private static int GetParamsCount(NameValueCollection sp)
        {
            int count = 0;

            if (sp["filterKategorie1"] != null || sp["filterKategorie2"] != null ||
                sp["filterKategorie3"] != null || sp["filterKategorie4"] != null || sp["filterKategorie5"] != null)
                count++;

            if (sp["filterMarke"] != null)
                count++;

            if (sp["filterFilterFarbe"] != null)
                count++;

            if (sp["filterAngebotArt"] != null)
                count++;

            return count;
        }
        private static string UeberschriftSeoAnpassung(string ueberschrift)
        {
            if (string.IsNullOrEmpty(ueberschrift))
                return "";
            ueberschrift = ueberschrift.Replace("Bekleidung", "Motorradbekleidung");
            ueberschrift = ueberschrift.Replace("-Motorradbekleidung", "-Bekleidung");
            ueberschrift = ueberschrift.Replace("Zubehör & Technik", "Motorrad-Zubehör & Technik"); // XX
            return ueberschrift;
        }
        #endregion
    }
}
