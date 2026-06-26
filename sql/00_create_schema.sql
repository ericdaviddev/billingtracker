DROP SCHEMA IF EXISTS billing CASCADE;

create schema billing;

create extension if not exists pgcrypto;