
# Full Stack Webshop School Project

## Overview
This project is a webshop application developed as part of a school exercise to learn about database management and REST API integration in a C# Razor environment. The application is structured into two main components: the backend, which handles database logic and API endpoints, and the frontend, which provides a user interface for interacting with the webshop.

## Technology Stack
- **Backend:** C#, ASP.NET Core, SQLite
- **Frontend:** HTML, CSS, JavaScript, Razor Views
- **Database:** SQLite

## Installation and Setup
1. **Clone the Repository:** Clone this repository to your local machine.
2. **Install ASP.NET Core:** Ensure ASP.NET Core is installed to run the application.
3. **NuGet Packages:** The project uses NuGet packages, including Microsoft.Data.Sqlite, which are automatically restored when you build the project. Ensure you have an internet connection during the first build.
4. **Running the Backend:**
   - Navigate to the backend project directory.
   - Run the application using `dotnet run`.
   - The backend should start on port `5198`.
5. **Running the Frontend:**
   - Open a new terminal in the frontend project directory.
   - Start the frontend using `dotnet watch`.
   - The frontend should be accessible on port `5020`.

*Note: SQLite integration is managed through NuGet packages within the application, eliminating the need for separate installation.*

### Configuring Port Usage
If the default ports (`5198` for the backend, `5020` for the frontend) are not available on your system, you can change them by:
- Adjusting the launch settings for the backend.
- Updating the `BaseUrl` in the `appsettings.json` file of the frontend to match the new backend port.

## Usage
The webshop application allows users to browse products, manage a shopping cart, and process purchases. Key features include:
- Adding and viewing products.
- Managing customer information.
- Handling shopping cart operations.
- Processing and viewing orders.

## Team Members
This project was collaboratively developed by a team of four coders. Below are the team members and their respective GitHub profiles:

- **Sofia Savolainen:** [GitHub Profile](https://github.com/osifa)
- **Annemari Humaloja-Mäkinen:** [GitHub Profile](https://github.com/AnnemariHM)
- **Toni Lehtonen:** [GitHub Profile](https://github.com/ToniSasky)
- **Anssi Peltola:** [GitHub Profile](https://github.com/AnssiPeltola)

Each member contributed to various aspects of the project, from database management to frontend development.

## License
This project is developed for educational purposes and is not licensed for commercial use.
