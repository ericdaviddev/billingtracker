--
-- PostgreSQL database dump
--

\restrict h9pvMZavqHen064VWAKIb03K1iNgwOKcOtq22o2HtjafH0cr43bBz6xoJkzYJ81

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: source_systems; Type: TABLE; Schema: billing; Owner: postgres
--

CREATE TABLE billing.source_systems (
    source_system_id uuid DEFAULT gen_random_uuid() NOT NULL,
    source_system_name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE billing.source_systems OWNER TO postgres;

--
-- Name: source_systems source_systems_pkey; Type: CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.source_systems
    ADD CONSTRAINT source_systems_pkey PRIMARY KEY (source_system_id);


--
-- PostgreSQL database dump complete
--

\unrestrict h9pvMZavqHen064VWAKIb03K1iNgwOKcOtq22o2HtjafH0cr43bBz6xoJkzYJ81

