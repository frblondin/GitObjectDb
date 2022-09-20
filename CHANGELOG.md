# Changelog
All notable changes to this project will be documented in this file. The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## 2022-09-20
- Completely revisited architecture: the model is no longer an immutable tree (cost, complexity), but rather a DTO-based model where queries are made by level. A proper cache mechanism with DTO being immutable does the rest
- Resources can be embedded within the serialization payload
- Resources can be stored in git sub-modules
- Use Nuke for builds
- Switch from Azure DevOps to GitHub Actions
- Add OData support
- Add GraphQL support
- Open for alternate serializer (other than Json based on System.Text.Json)

## 2019-12-15
- Old 0.1 experiment