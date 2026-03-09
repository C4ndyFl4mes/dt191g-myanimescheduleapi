# My Anime Schedule - API

## Kom igång
Du behöver .NET 10 och Docker för att köra API:et.

#### Instruktioner:
1. `git clone https://github.com/C4ndyFl4mes/dt191g-myanimescheduleapi.git myanimeschedule`
2. Döp om example.env till .env och ange värden, spelar ingen roll vad de är så länge de funkar.
3. `docker compose up -d`
4. Indexeringen börjar på en gång, den tar ca en minut.
5. Klart.


## Beskrivning
API:et hanterar inlägg, användare, scheman och indexering. Indexeringen sker som en Background Service med PeriodicTimer som ansvarar för att tabellen för animes håller sig uppdaterad var fjärde timme. Databasen är MariaDB och servas till API:et genom Docker.

En användare kan logga in och skapa inlägg på animes, lägga till animes i vederbörandes schema och mer. API:et fungerar bäst med frontend applikationen som är en Blazor WebAssembly, [klicka här](https://github.com/C4ndyFl4mes/dt191g-myanimescheduleblazor) för att komma till det repot.

### Ändpunkter
Det finns 15 ändpunkter varav en (signout) är oanvändbar och hade ett syfte när applikationen använde cookies.

#### Inlägg (api/posts)
<table>
    <thead>
        <tr>
            <th>Metod</th>
            <th>Ändpunkt</th>
            <th>Autentisering</th>
            <th>Body</th>
            <th>Beskrivning</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>GET</td>
            <td>/{targetID}/{page}?timezone={timezone}</td>
            <td>X</td>
            <td>X</td>
            <td>Hämtar inlägg och anger den korrekta lokala tiden.</td>
        </tr>
        <tr>
            <td>POST</td>
            <td>/send</td>
            <td>Member</td>
            <td>PostRequest</td>
            <td>Skickar ett inlägg till en måltråd.</td>
        </tr>
        <tr>
            <td>PUT</td>
            <td>/edit</td>
            <td>Member</td>
            <td>PostRequest</td>
            <td>Redigerar ett befintligt inlägg i måltråden.</td>
        </tr>
        <tr>
            <td>DELETE</td>
            <td>/delete/{targetID}</td>
            <td>Member</td>
            <td>X</td>
            <td>Tar bort ett inlägg från måltråden.</td>
        </tr>
    </tbody>
</table>


#### Schema (api/schedule)
<table>
    <thead>
        <tr>
            <th>Metod</th>
            <th>Ändpunkt</th>
            <th>Autentisering</th>
            <th>Body</th>
            <th>Beskrivning</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>GET</td>
            <td>/schedule</td>
            <td>Member</td>
            <td>X</td>
            <td>Hämtar den inloggade användarens schema.</td>
        </tr>
        <tr>
            <td>POST</td>
            <td>/entry</td>
            <td>Member</td>
            <td>ScheduleRequest</td>
            <td>Lägger till en anime i användarens schema.</td>
        </tr>
        <tr>
            <td>PUT</td>
            <td>/entry</td>
            <td>Member</td>
            <td>ScheduleUpdateRequest</td>
            <td>Uppdaterar en befintlig schemapost.</td>
        </tr>
        <tr>
            <td>DELETE</td>
            <td>/entry/{indexedAnimeId}</td>
            <td>Member</td>
            <td>X</td>
            <td>Tar bort en schemapost från användarens schema.</td>
        </tr>
    </tbody>
</table>

#### Användare (api/user)
<table>
    <thead>
        <tr>
            <th>Metod</th>
            <th>Ändpunkt</th>
            <th>Autentisering</th>
            <th>Body</th>
            <th>Beskrivning</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>POST</td>
            <td>/signup</td>
            <td>X</td>
            <td>SignUpRequest</td>
            <td>Skapar en ny användare och returnerar profilinformation.</td>
        </tr>
        <tr>
            <td>POST</td>
            <td>/signin</td>
            <td>X</td>
            <td>SignInRequest</td>
            <td>Loggar in en användare och returnerar profilinformation.</td>
        </tr>
        <tr>
            <td>POST</td>
            <td>/signout</td>
            <td>X</td>
            <td>X</td>
            <td>Loggar ut den inloggade användaren (oanvänd i nuvarande implementation).</td>
        </tr>
        <tr>
            <td>DELETE</td>
            <td>/{targetID}</td>
            <td>Moderator</td>
            <td>X</td>
            <td>Tar bort en användare med moderatorbehörighet.</td>
        </tr>
        <tr>
            <td>GET</td>
            <td>/info/{page}?targetID={targetID}&timezone={timezone}</td>
            <td>Member</td>
            <td>X</td>
            <td>Hämtar användarinfo och paginerade inlägg för en användare.</td>
        </tr>
        <tr>
            <td>PUT</td>
            <td>/settings</td>
            <td>Member</td>
            <td>UserSettings</td>
            <td>Uppdaterar den inloggade användarens inställningar.</td>
        </tr>
        <tr>
            <td>GET</td>
            <td>/list?page={page}</td>
            <td>Moderator</td>
            <td>X</td>
            <td>Hämtar en paginerad lista över användare.</td>
        </tr>
    </tbody>
</table>