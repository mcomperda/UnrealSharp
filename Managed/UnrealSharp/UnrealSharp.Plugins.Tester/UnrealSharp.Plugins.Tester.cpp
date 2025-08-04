// UnrealSharp.Plugins.Tester.cpp : This file contains the 'main' function. Program execution begins and ends there.
//
#include <windows.h>
#include <iostream>

typedef int (*Start)();
typedef void (*Shutdown)();

bool startLibrary(HMODULE hModule)
{
	try
	{
		Start start = (Start)GetProcAddress(hModule, "Start");
		if (start == NULL)
		{
			std::cerr << "Failed to get function address." << std::endl;
			FreeLibrary(hModule);
			return false;
		}

		int result = start(); // Call the function with example arguments
		return result == 1;
	}
	catch (...) {
		return false;
	}
}

bool shutdownLibrary(HMODULE hModule)
{

	Shutdown shutdown = (Shutdown)GetProcAddress(hModule, "Shutdown");
	if (shutdown == NULL)
	{
		std::cerr << "Failed to get function address." << std::endl;
		FreeLibrary(hModule);
		return false;
	}

	shutdown();

}

typedef byte(*InitializeUnrealSharp)(const char* workingDirectoryPath, const char* assemblyPath, void* pluginCallbacks, void* bindsCallbacks, void* managedCallbacks);

bool initializeUnrealSharp(HMODULE hModule)
{
	try
	{
		auto addr = GetProcAddress(hModule, "InitializeUnrealSharp");
		InitializeUnrealSharp init = (InitializeUnrealSharp)addr;
		if (init == NULL)
		{
			std::cerr << "Failed to get function address." << std::endl;
			FreeLibrary(hModule);
			return false;
		}
		const char* workingDirectoryPath = "C:\\Path\\To\\Your\\WorkingDirectory"; // Example path		
		byte result = init(workingDirectoryPath, 0, 0, 0, 0); // Call the function with example arguments
		return result == 15;
	}
	catch (...) {
		return false;
	}
}

int main()
{
	HMODULE hModule = LoadLibrary(L"P:\\UnrealProjects\\Beacon\\Plugins\\UnrealSharp\\Binaries\\Win64\\UnrealSharp.Plugins.dll");
	if (hModule == NULL)
	{
		std::cerr << "Failed to load DLL." << std::endl;
		return 1;
	}

	if (!startLibrary(hModule))
	{
		std::cerr << "Library test failed." << std::endl;
	}

	if (!initializeUnrealSharp(hModule))
	{
		std::cerr << "UnrealSharp initialization failed." << std::endl;
	}


	shutdownLibrary(hModule); // Stop the library before freeing it	

	FreeLibrary(hModule); // Ensure the DLL is freed    

	std::cout << "Hello World!\n";
}

