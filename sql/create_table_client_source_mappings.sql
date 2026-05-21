--
-- PostgreSQL database dump
--

\restrict 7LVUuMeDaX8D3tw3YQvY6fmR2whWYaYWTeVi7DkLJGwV1fmkGUSbEVM1fg447bf

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
-- Name: client_source_mappings; Type: TABLE; Schema: billing; Owner: postgres
--

CREATE TABLE billing.client_source_mappings (
    mapping_id uuid DEFAULT gen_random_uuid() NOT NULL,
    client_id uuid NOT NULL,
    source_system_id uuid NOT NULL,
    external_client_id text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE billing.client_source_mappings OWNER TO postgres;

--
-- Name: client_source_mappings client_source_mappings_pkey; Type: CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.client_source_mappings
    ADD CONSTRAINT client_source_mappings_pkey PRIMARY KEY (mapping_id);


--
-- Name: client_source_mappings client_source_mappings_unique_constraint; Type: CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.client_source_mappings
    ADD CONSTRAINT client_source_mappings_unique_constraint UNIQUE (client_id, source_system_id, external_client_id);


--
-- Name: client_source_mappings client_source_mappings_clients_client_id_fk; Type: FK CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.client_source_mappings
    ADD CONSTRAINT client_source_mappings_clients_client_id_fk FOREIGN KEY (client_id) REFERENCES billing.clients(client_id);


--
-- Name: client_source_mappings client_source_mappings_source_systems_source_system_id_fk; Type: FK CONSTRAINT; Schema: billing; Owner: postgres
--

ALTER TABLE ONLY billing.client_source_mappings
    ADD CONSTRAINT client_source_mappings_source_systems_source_system_id_fk FOREIGN KEY (source_system_id) REFERENCES billing.source_systems(source_system_id);


--
-- PostgreSQL database dump complete
--

\unrestrict 7LVUuMeDaX8D3tw3YQvY6fmR2whWYaYWTeVi7DkLJGwV1fmkGUSbEVM1fg447bf

