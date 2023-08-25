#r "nuget: FSharp.Data"
open FSharp.Data
open System.IO
open System

let models = [|
    "Renault Clio"
    "Renault Megane"
    "Renault Captur"
    "Renault Kangoo"
    "Renault Scenic"
    "Renault Laguna"
    "Renault Austral"
    "Renault Espace"
    "Renault Twingo"
    "Renault Talisman"
    "Renault Fluence"
    "Renault Arkana"
    "Renault Kadjar"
    "Renault Kol"
|]
let nbModels = models |> Array.length

type GeoNameApi = JsonProvider<"https://public.opendatasoft.com/api/explore/v2.1/catalog/datasets/geonames-all-cities-with-a-population-500/records?select=country%2C%20name&where=timezone%20like%20%27europe%25%27&order_by=population%20desc&limit=3">

let GetCities (offset) (limit: int) : GeoNameApi.Result[] =
    let baseUrl = "https://public.opendatasoft.com/api/explore/v2.1/catalog/datasets/geonames-all-cities-with-a-population-500/records?select=country%2C%20name&where=timezone%20like%20%27europe%25%27&order_by=population%20desc"
    let url = sprintf "%s&offset=%d&limit=%d" baseUrl offset limit
    let result = GeoNameApi.Load(url)
    let cities = result.Results
    cities

let random = new Random()

let WriteModels filename writedRows (city: GeoNameApi.Result) =
    use writer = new StreamWriter(filename, true)
    models
    |> Array.indexed
    |> Array.iter (fun (i, model) ->
        let rowIndex = i + writedRows
        sprintf "%d;%s;%s;%s;%d;%d" rowIndex city.Country city.Name model (random.Next(600,1000)) (random.Next(900, 1200))
        |> writer.WriteLine)
    writedRows + models.Length

let WriteCities filename writedRows (cities: GeoNameApi.Result[]) =
    cities
    |> Array.indexed
    |> Array.map (fun (index, city) -> WriteModels filename ((index * nbModels) + writedRows) city)
    |> Array.max

let InitFile filename = 
    use writer = new StreamWriter(filename, false)
    writer.WriteLine "RowId;Region;Secteur;Produit;Actuel;Objectif"

let rec FillFile expectedNbCities filename (writedCities, writedRows) = 
    if (writedCities < expectedNbCities) then
        let nbMissingCities = expectedNbCities - writedCities
        let cities = 
            match nbMissingCities < 100 with
            | true -> GetCities writedCities nbMissingCities
            | false -> GetCities writedCities 100
        
        let writedRows =
            cities
            |> WriteCities filename writedRows

        (writedCities + cities.Length, writedRows)
        |> FillFile expectedNbCities filename


let GenerateFile expectedNbRows filename = 
    InitFile filename
    let expectedNbCities = expectedNbRows / nbModels
    FillFile expectedNbCities filename (0, 0)

GenerateFile 100000 "MeasureMassData\\renaultRsO_100000.csv"