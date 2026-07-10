#include "MainModule.h"

#include <cstdio>

MainModule::MainModule()
{
}

MainModule::~MainModule()
{
}

void MainModule::Run()
{
	printf("hello world!");
}

int main()
{
	MainModule entry;
	entry.Run();
	return 0;
}