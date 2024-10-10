[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Cabazure/Cabazure.Kusto/.github%2Fworkflows%2Fci.yml)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![GitHub Release Date](https://img.shields.io/github/release-date/Cabazure/Cabazure.Kusto)](https://github.com/Cabazure/Cabazure.Kusto/releases)
[![NuGet Version](https://img.shields.io/nuget/v/Cabazure.Kusto?color=blue)](https://www.nuget.org/packages/Cabazure.Kusto)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cabazure.Kusto?color=blue)](https://www.nuget.org/stats/packages/Cabazure.Kusto?groupby=Version)

[![Branch Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_branchcoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![Line Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_linecoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![Method Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_methodcoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)

# Cabazure.Kusto

Cabazure.Kusto is a library for executing kusto scripts against an Azure Data Explorer cluster (ADX). The library extents the official .NET SDK by adding functionality for:
 * handling embedded .kusto scripts in your .NET projects
 * passing parameters to your .kusto scripts
 * deserialization and pagination of query results
